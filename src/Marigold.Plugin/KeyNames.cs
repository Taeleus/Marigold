namespace Marigold
{
    internal static class KeyNames
    {
        public static string Normalize(string value)
        {
            value = (value ?? "").Trim();
            if (value.EndsWith("+"))
                value = value.Substring(0, value.Length - 1) + "OemPlus";
            int split = value.LastIndexOf('+');
            string prefix = split < 0 ? "" : value.Substring(0, split + 1), key = value.Substring(split + 1);
            switch (key)
            {
                case "'":
                case "apostrophe":
                    key = "OemQuotes";
                    break;
                case ";":
                    key = "OemSemicolon";
                    break;
                case ",":
                    key = "OemComma";
                    break;
                case ".":
                    key = "OemPeriod";
                    break;
                case "-":
                    key = "OemMinus";
                    break;
                case "=":
                    key = "OemPlus";
                    break;
                case "/":
                    key = "OemQuestion";
                    break;
                case "`":
                    key = "OemTilde";
                    break;
                case "[":
                    key = "OemOpenBrackets";
                    break;
                case "]":
                    key = "OemCloseBrackets";
                    break;
                case "\\":
                    key = "OemPipe";
                    break;
            }

            if (key.Length == 1 && key[0] >= '0' && key[0] <= '9')
                key = "D" + key;
            return prefix + key;
        }
    }
}
