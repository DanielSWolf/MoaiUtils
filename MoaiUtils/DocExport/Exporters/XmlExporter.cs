using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using MoaiUtils.MoaiParsing;
using MoaiUtils.MoaiParsing.CodeGraph;
using MoaiUtils.Tools;

namespace MoaiUtils.DocExport.Exporters {
    public class XmlExporter : IApiExporter {
        public void Export(IEnumerable<MoaiType> types, string header, DirectoryInfo outputDirectory) {
            // Create XML DOM
            var document = new XDocument(
                new XComment(header),
                new XElement("types", types.Select(CreateTypeElement))
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

        private XElement CreateTypeElement(MoaiType type) {
            return new XElement("type",
                new XAttribute("name", type.Name),
                new XElement("baseTypes",
                    type.BaseTypes.Select(baseType => new XElement("baseType", baseType.Name))
                ),
                new XElement("description", type.Description),
                new XElement("members", type.Members.Select(member => (XElement) CreateMemberElement((dynamic) member)))
            );
        }

        private XElement CreateMemberElement(MoaiField field) {
            var fieldTypes = new Dictionary<Type, string> {
                { typeof(MoaiConstant), "constant" },
                { typeof(MoaiFlag), "flag" },
                { typeof(MoaiAttribute), "attribute" }
            };

            return new XElement("field",
                new XAttribute("type", fieldTypes[field.GetType()]),
                new XAttribute("name", field.Name),
                new XElement("description", field.Description)
            );
        }

        private XElement CreateMemberElement(MoaiMethod method) {
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

        private XElement CreateOverloadElement(MoaiMethodOverload overload) {
            return new XElement("overload",
                new XAttribute("static", overload.IsStatic),
                new XElement("inParams", overload.InParameters.Select(CreateInParamElement)),
                new XElement("outParams", overload.OutParameters.Select(CreateOutParamElement))
            );
        }

        private XElement CreateInParamElement(MoaiInParameter param) {
            return new XElement("inParam",
                new XAttribute("name", param.Name ?? string.Empty),
                new XAttribute("type", param.Type.Name),
                new XAttribute("optional", param.IsOptional),
                new XElement("description", param.Description)
            );
        }

        private XElement CreateOutParamElement(MoaiOutParameter param) {
            return new XElement("outParam",
                new XAttribute("name", param.Name ?? string.Empty),
                new XAttribute("type", param.Type.Name),
                new XElement("description", param.Description)
            );
        }
    }
}