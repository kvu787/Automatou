using System.Globalization;
using System.Text;

namespace Automatou.TerminalShell;

internal sealed class CommandInput {
    private readonly List<string> _words = [];

    public CommandInput(string line) {
        StringBuilder word = new();
        char quote = '\0';
        bool started = false;
        for (int index = 0; index < line.Length; index++) {
            char character = line[index];
            if (quote != '\0') {
                if (character == quote) {
                    quote = '\0';
                } else if (character == '\\' && index + 1 < line.Length &&
                         (line[index + 1] == quote || line[index + 1] == '\\')) {
                    _ = word.Append(line[++index]);
                } else {
                    _ = word.Append(character);
                }
            } else if (character is '"' or '\'') {
                quote = character;
                started = true;
            } else if (char.IsWhiteSpace(character)) {
                if (started) {
                    this._words.Add(word.ToString());
                    _ = word.Clear();
                    started = false;
                }
            } else {
                _ = word.Append(character);
                started = true;
            }
        }

        if (quote != '\0') {
            throw new ArgumentException("Unclosed quote. Put matching quotes around names containing spaces.");
        }

        if (started) {
            this._words.Add(word.ToString());
        }
    }

    public string Name => this._words.Count == 0 ? "" : this._words[0].ToLowerInvariant();
    public int Count => Math.Max(0, this._words.Count - 1);

    public void RequireCount(int minimum, int maximum, string usage) {
        if (this.Count < minimum || this.Count > maximum) {
            throw new ArgumentException($"Usage: {usage}");
        }
    }

    public int Integer(int index, string name) {
        return int.TryParse(this._words[index + 1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int value)
            ? value
            : throw new ArgumentException($"{name} must be a whole number.");
    }

    public long LongInteger(int index, string name) {
        return long.TryParse(this._words[index + 1], NumberStyles.Integer, CultureInfo.InvariantCulture, out long value)
            ? value
            : throw new ArgumentException($"{name} must be a 64-bit whole number.");
    }

    public T EnumName<T>(int index) where T : struct, Enum {
        string text = this._words[index + 1];
        return Enum.GetNames<T>().Any(name => name.Equals(text, StringComparison.OrdinalIgnoreCase))
            ? Enum.Parse<T>(text, ignoreCase: true)
            : throw new ArgumentException($"Unknown {typeof(T).Name} '{text}'. Choose: {string.Join(", ", Enum.GetNames<T>())}.");
    }

    public string RemainingText(int index) {
        return string.Join(" ", this._words.Skip(index + 1));
    }
}
