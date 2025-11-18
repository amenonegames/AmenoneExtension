namespace Data.Utils
{
    public static class IntExtensions
    {
        public static int NoToIndex(this int no)
        {
            var index = no > 0 ? no - 1 : 0;
            return index;
        }

        public static int IndexToNo(this int index)
        {
            var no = index + 1;
            return no;
        }
    }
}