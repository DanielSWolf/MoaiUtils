namespace MoaiUtils.MoaiParsing {
    public static class MOAILuaObject {
        public const string DummyCode = @"
/** @lua    MOAILuaObject
    @text   Base class for all of Moai's Lua classes.
*/
class MOAILuaObject {}

/** @lua    getClass
*/
int MOAILuaObject::_getClass(lua_State* L) {
    ...
}

/** @lua    getClassName
    @text   Return the class name for the current object.
    @in     MOAILuaObject self
    
    @out    string
*/
int MOAILuaObject::_getClassName(lua_State* L) {
    ...
}

/** @lua    setInterface
*/
int MOAILuaObject::_setInterface(lua_State* L) {
    ...
}";
    }
}