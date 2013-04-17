using System;
using System.Reflection;

namespace Inedo.BuildMasterExtensions
{
    internal static class MungeUtil
    {
        static Type[] objsToTyps(object[] objs)
        {
            if (objs == null) return null;
            var typs = new Type[objs.Length];
            for (int i = 0; i < typs.Length; i++)
                typs[i] = objs[i] == null ? null : objs[i].GetType();
            return typs;
        }

        public static object InvokeMethod(string assemblyQualifiedTypeName, string methodName, params object[] args)
        {
            var typ = Type.GetType(assemblyQualifiedTypeName, true);
            return typ
                .GetMethod(
                    methodName, 
                    BindingFlags.FlattenHierarchy | BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public | BindingFlags.Static,
                    Type.DefaultBinder,
                    objsToTyps(args),
                    null)
                .Invoke(typ.IsAbstract ? null : Activator.CreateInstance(typ), args);
        }

        public static object GetPropertyValue(object objToMunge, string propertyName)
        {
            return objToMunge.GetType().GetProperty(propertyName).GetValue(objToMunge, null);
        }

        public static object MungeInstance(string assemblyQualifiedTypeName, object objToMunge)
        {
            if (objToMunge == null) throw new ArgumentNullException("objToMunge");
            var obj = Activator.CreateInstance(Type.GetType(assemblyQualifiedTypeName, true));
            foreach (PropertyInfo mungeProp in objToMunge.GetType().GetProperties())
                foreach (PropertyInfo prop in obj.GetType().GetProperties())
                    if (prop.Name == mungeProp.Name)
                        prop.SetValue(obj, mungeProp.GetValue(objToMunge, null), null);
            return obj;
        }

        public static Inedo.BuildMaster.Extensibility.Actions.ActionBase MungeCoreExAction(string partialTypeName, object objToMunge)
        {
            if (!partialTypeName.StartsWith("Inedo.BuildMaster.Extensibility.Actions."))
                partialTypeName = "Inedo.BuildMaster.Extensibility.Actions." + partialTypeName;
            if (!partialTypeName.EndsWith(",BuildMasterExtensions"))
                partialTypeName += ",BuildMasterExtensions";

            return (Inedo.BuildMaster.Extensibility.Actions.ActionBase)MungeInstance(partialTypeName, objToMunge);
        }

        public static Inedo.BuildMaster.Extensibility.Variables.VariableBase MungeCoreExVariable(string partialTypeName, object objToMunge)
        {
            if (!partialTypeName.StartsWith("Inedo.BuildMaster.Extensibility.Variables."))
                partialTypeName = "Inedo.BuildMaster.Extensibility.Variables." + partialTypeName;
            if (!partialTypeName.EndsWith(",BuildMasterExtensions"))
                partialTypeName += ",BuildMasterExtensions";

            return (Inedo.BuildMaster.Extensibility.Variables.VariableBase)MungeInstance(partialTypeName, objToMunge);
        }

        public static Inedo.BuildMaster.Extensibility.Predicates.PredicateBase MungeCoreExPredicate(string partialTypeName, object objToMunge)
        {
            if (!partialTypeName.StartsWith("Inedo.BuildMaster.Extensibility.Predicates."))
                partialTypeName = "Inedo.BuildMaster.Extensibility.Predicates." + partialTypeName;
            if (!partialTypeName.EndsWith(",BuildMasterExtensions"))
                partialTypeName += ",BuildMasterExtensions";

            return (Inedo.BuildMaster.Extensibility.Predicates.PredicateBase)MungeInstance(partialTypeName, objToMunge);
        }
    }
}
