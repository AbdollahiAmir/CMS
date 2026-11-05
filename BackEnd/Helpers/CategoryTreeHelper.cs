namespace BackEnd.Helpers
{
    public static class CategoryTreeHelper
    {
        public static HashSet<int> GetDescendantIds(IEnumerable<(int Id, int? ParentId)> nodes, int rootId)
        {
            var lookup = nodes
                .Where(n => n.ParentId.HasValue)
                .ToLookup(n => n.ParentId!.Value, n => n.Id);

            var result = new HashSet<int>();
            var stack = new Stack<int>();
            stack.Push(rootId);

            while (stack.Count > 0)
            {
                var current = stack.Pop();

                foreach (var child in lookup[current])
                {
                    if (result.Add(child))
                    {
                        stack.Push(child);
                    }
                }
            }

            return result;
        }
    }
}