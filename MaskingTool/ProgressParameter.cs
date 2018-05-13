namespace MaskingTool
{
    class ProgressParameter
    {
        public int Max { get; private set; }
        public int Current { get; private set; }

        public ProgressParameter(int max, int current)
        {
            this.Max = max;
            this.Current = current;
        }
    }
}
