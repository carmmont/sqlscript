using Microsoft.SqlServer.Management.Smo;
using System.Collections.Generic;

public class dependency_index
{
    internal Dictionary<string, List<string>> _index = new Dictionary<string, List<string> >();

    internal void add(string child, string parent)
    {
        if(!_index.ContainsKey(child))
        {
            _index[child] = new List<string>();
        }

        if(!_index[child].Contains(parent))
        {
            _index[child].Add(parent);
        }
    }

    public string[] get_parents(string urn)
    {
        if(!_index.ContainsKey(urn))
            return null;

        return _index[urn].ToArray();
    }
}

public class dependency
{
    private static List<DependencyTreeNode> walk(DependencyTreeNode parent, dependency_index index)
    {
        if(!parent.HasChildNodes)
            return null;

        List<DependencyTreeNode> children = new List<DependencyTreeNode>(); 

        DependencyTreeNode child = parent.FirstChild;

        while(null != child)
        {
            index.add(child.Urn.Value, parent.Urn.Value);
            //walk(child, index);

            children.Add(child);

            child = child.NextSibling;
        }

        return children;
        
    }
    public static dependency_index index(DependencyTree tr)
    {
        
        dependency_index index = new dependency_index();

        DependencyTreeNode child = tr.FirstChild;

        while(null != child)
        {
            List<DependencyTreeNode> children = walk(child, index);
            if(null != children)
            {
                foreach(DependencyTreeNode parent in children)
                {
                    walk(parent, index);
                }
            }

            child = child.NextSibling;
        }

        return index;

        
    }
}