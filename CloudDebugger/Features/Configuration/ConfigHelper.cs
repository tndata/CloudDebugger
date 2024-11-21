namespace CloudDebugger.Features.Configuration;

/// <summary>
/// This helper class is currently not used.
/// 
/// This class provides a helper method to retrieve all configuration 
/// keys and their values from an IConfigurationRoot object.
/// </summary>
public static class ConfigHelper
{
    public static List<string> AllConfigurationKeys(this IConfigurationRoot root)
    {
        (string? Value, IConfigurationProvider? Provider) GetValueAndProvider(
                                                            IConfigurationRoot root,
                                                            string key)
        {
            foreach (IConfigurationProvider provider in root.Providers.Reverse())
            {
                if (provider.TryGet(key, out string? value))
                {
                    return (value ?? "", provider);
                }
            }

            return (null, null);
        }
        var keys = new HashSet<string>();
        RecurseChildren(keys, root.GetChildren(), "");
        return keys.ToList();


        void RecurseChildren(
                HashSet<string> keys,
                IEnumerable<IConfigurationSection> children, string rootPath)
        {
            foreach (IConfigurationSection child in children)
            {
                (string? Value, IConfigurationProvider? Provider) = GetValueAndProvider(root, child.Path);

                if (Provider != null)
                {
                    if (string.IsNullOrEmpty(rootPath))
                    {
                        keys.Add(child.Key + "=" + child.Value);
                    }
                    else
                    {
                        keys.Add(rootPath + ":" + child.Key + "=" + child.Value);
                    }
                }

                RecurseChildren(keys, child.GetChildren(), child.Path);
            }
        }
    }
}
