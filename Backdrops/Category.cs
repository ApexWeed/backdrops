namespace Backdrops
{
    public class Category
    {
        public int CategoryID;
        public string CategoryName;
        public string CategoryIconUrl;

        public override string ToString()
        {
            return CategoryName;
        }
    }
}
