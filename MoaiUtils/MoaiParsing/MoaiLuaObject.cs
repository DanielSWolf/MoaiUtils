namespace MoaiUtils.MoaiParsing {
    public static class MOAILuaObject {
        public const string DummyCode = @"
/** @name   MOAILuaObject
    @text   Base class for all of Moai's Lua classes.
*/
class MOAILuaObject {}

/** @name   getClass
*/
int MOAILuaObject::_getClass(lua_State* L) {
    ...
}

/** @name   getClassName
    @text   Return the class name for the current object.
    @in     MOAILuaObject self
    
    @out    string
*/
int MOAILuaObject::_getClassName(lua_State* L) {
    ...
}

/** @name   setInterface
*/
int MOAILuaObject::_setInterface(lua_State* L) {
    ...
}";
    }
}