using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace NAVIGEST.Android.Models;

public class ProductGroup : ObservableCollection<Product>
{
    public string Title { get; }

    public ProductGroup(string title) => Title = title;

    public ProductGroup(string title, IEnumerable<Product> items) : base(items)
    {
        Title = title;
    }
}
