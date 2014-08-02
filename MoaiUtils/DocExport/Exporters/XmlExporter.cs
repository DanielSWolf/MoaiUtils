using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using MoaiUtils.MoaiParsing;
using MoaiUtils.MoaiParsing.CodeGraph;
using MoaiUtils.MoaiParsing.CodeGraph.Types;
using MoaiUtils.Tools;
using Attribute = MoaiUtils.MoaiParsing.CodeGraph.Attribute;

namespace MoaiUtils.DocExport.Exporters {
    public class XmlExporter : IApiExporter {
        public void Export(MoaiClass[] classes, string header, DirectoryInfo outputDirectory) {
            // Create XML DOM
            var document = new XDocument(
                new XComment(header),
                new XElement("types", classes.OrderBy(type => type.Name).Select(CreateClassElement))
            );

            // Save it
            string outputFileName = outputDirectory.GetFileInfo("moai.xml").FullName;
            XmlWriterSettings settings = new XmlWriterSettings {
                Indent = true,
                IndentChars = "\t"
            };
            using (XmlWriter writer = XmlWriter.Create(outputFileName, settings)) {
                document.Save(writer);
            }
        }

        private XElement CreateClassElement(MoaiClass moaiClass) {
            return new XElement("type",
                new XAttribute("name", moaiClass.Name),
                new XAttribute("scriptable", moaiClass.IsScriptable),
                new XElement("baseTypes",
                    moaiClass.BaseClasses.Select(baseClass => new XElement("baseType", baseClass.Name))
                ),
                new XElement("description", moaiClass.Description),
                new XElement("members", moaiClass.Members.OrderBy(member => member.Name).Select(member => (XElement) CreateMemberElement((dynamic) member)))
            );
        }

        private XElement CreateMemberElement(Field field) {
            var fieldTypes = new Dictionary<System.Type, string> {
                { typeof(Constant), "constant" },
                { typeof(Flag), "flag" },
                { typeof(Attribute), "attribute" }
            };

            return new XElement("field",
                new XAttribute("type", fieldTypes[field.GetType()]),
                new XAttribute("name", field.Name),
                new XElement("description", field.Description)
            );
        }

        private XElement CreateMemberElement(Method method) {
            return new XElement("method",
                new XAttribute("name", method.Name),
                new XElement("description", method.Description),
                new XElement("overloads", method.Overloads.Select(CreateOverloadElement)),
                new XElement("compactSignature",
                    new XElement("inParams", method.InParameterSignature != null ? method.InParameterSignature.ToString(SignatureGrouping.Any) : null),
                    new XElement("outParams", method.OutParameterSignature != null ? method.OutParameterSignature.ToString(SignatureGrouping.Any) : null)
                )
            );
        }

        private XElement CreateOverloadElement(MethodOverload overload) {
            return new XElement("overload",
                new XAttribute("static", overload.IsStatic),
                new XElement("inParams", overload.InParameters.Select(CreateInParamElement)),
                new XElement("outParams", overload.OutParameters.Select(CreateOutParamElement))
            );
        }

        private XElement CreateInParamElement(InParameter param) {
            return new XElement("inParam",
                new XAttribute("name", param.Name ?? string.Empty),
                new XAttribute("type", param.Type.Name),
                new XAttribute("optional", param.IsOptional),
                new XElement("description", param.Description)
            );
        }

        private XElement CreateOutParamElement(OutParameter param) {
            return new XElement("outParam",
                new XAttribute("name", param.Name ?? string.Empty),
                new XAttribute("type", param.Type.Name),
                new XElement("description", param.Description)
            );
        }
    }
}