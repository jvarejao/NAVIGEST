import re
import os

resx_path = '/Users/joaovarejao/Dev/NAVIGEST/src/NAVIGEST.Shared/Resources/Strings/AppResources.resx'
designer_path = '/Users/joaovarejao/Dev/NAVIGEST/src/NAVIGEST.Shared/Resources/Strings/AppResources.Designer.cs'

def get_resx_keys(path):
    with open(path, 'r', encoding='utf-8') as f:
        content = f.read()
    return re.findall(r'<data name="([^"]+)"', content)

def get_designer_keys(path):
    with open(path, 'r', encoding='utf-8') as f:
        content = f.read()
    return re.findall(r'public static string ([^\s]+) {', content)

def update_designer():
    resx_keys = set(get_resx_keys(resx_path))
    designer_keys = set(get_designer_keys(designer_path))
    
    missing_keys = resx_keys - designer_keys
    
    if not missing_keys:
        print("No missing keys found.")
        return

    print(f"Found {len(missing_keys)} missing keys.")
    
    new_properties = []
    for key in sorted(missing_keys):
        prop = f"""
        public static string {key} {{
            get {{
                return ResourceManager.GetString("{key}", resourceCulture);
            }}
        }}"""
        new_properties.append(prop)
    
    with open(designer_path, 'r', encoding='utf-8') as f:
        content = f.read()
    
    # Find the last closing brace of the class
    # The file ends with two closing braces (one for class, one for namespace)
    # We want to insert before the class closing brace.
    
    # A simple heuristic: find the last "}" and insert before the one before it?
    # Or just find the last "}" and assume it's the namespace, and the one before is the class.
    
    # Let's look at the end of the file.
    # It usually looks like:
    #     }
    # }
    
    last_brace_index = content.rfind('}')
    second_last_brace_index = content.rfind('}', 0, last_brace_index)
    
    if second_last_brace_index == -1:
        print("Could not find insertion point.")
        return

    # Insert before the second to last brace
    new_content = content[:second_last_brace_index] + "\n" + "\n".join(new_properties) + "\n    " + content[second_last_brace_index:]
    
    with open(designer_path, 'w', encoding='utf-8') as f:
        f.write(new_content)
    
    print("Updated AppResources.Designer.cs")

if __name__ == "__main__":
    update_designer()
