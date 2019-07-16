using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

public static class AttributeExtensions
{
    public static TAttribute GetCustomTypeAttribute<TAttribute>(this Enum parameter)
        where TAttribute : Attribute
    {
        return GetCustomTypeAttributes<TAttribute>(parameter)?.FirstOrDefault();
    }

    public static List<TAttribute> GetCustomTypeAttributes<TAttribute>(this Enum parameter)
        where TAttribute : Attribute
    {
        var memberInfo = parameter?.GetType().GetMember(parameter.ToString()).FirstOrDefault();
        if (memberInfo == null)
        {
            return null;
        }
        return Attribute.GetCustomAttributes(memberInfo, typeof(TAttribute))
            .Cast<TAttribute>()
            .ToList();
    }

    public static List<(Type type, TAttribute attribute)> GetAssemblyTypes<TAttribute>()
        where TAttribute : Attribute
    {
        return Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(x => x.IsDefined(typeof(TAttribute), true))
            .Select(x => (type: x, attribute: x.GetCustomAttribute<TAttribute>()))
            .ToList();
    }

    private static Dictionary<Type, List<(Type enumType, List<(string enumName, List<Attribute> attributes)> matches)>> _assemblyEnumMemberCache =
        new Dictionary<Type, List<(Type enumType, List<(string enumName, List<Attribute> attributes)> matches)>>();

    public static List<(Type enumType, List<(string enumName, List<Attribute> attributes)> matches)> GetAssemblyEnumMembers<TAttribute>()
        where TAttribute : Attribute
    {
        if (_assemblyEnumMemberCache.ContainsKey(typeof(TAttribute)))
        {
            return _assemblyEnumMemberCache[typeof(TAttribute)];
        }

        //AppDomain.CurrentDomain.GetAssemblies()
        var enumMembers = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(x => x.IsEnum && x.IsPublic)
            .Select(enumType =>
            {
                var matches = (Enum.GetValues(enumType) as int[])
                    .Select(y =>
                    {
                        var enumName = Enum.GetName(enumType, y);
                        var attributes = enumType.GetMember(enumName)[0]
                            .GetCustomAttributes(typeof(TAttribute), false)
                            .Cast<Attribute>()
                            .ToList();
                        return (enumName, attributes);
                    })
                    .Where(y => y.attributes.HasItems())
                    .ToList();
                return (enumType, matches);
            })
            .ToList();

        _assemblyEnumMemberCache.Add(typeof(TAttribute), enumMembers);
        return enumMembers;
    }
}
