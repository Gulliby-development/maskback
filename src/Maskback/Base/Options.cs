namespace Maskback.Base;

public static class Options
{
    public static Parsed Parse(IReadOnlyList<string> args, string[] flags, string[] switches)
    {
        var parsed = new Parsed();
        var terminated = false;

        for (var index = 0; index < args.Count; index++)
        {
            var token = args[index];

            if (terminated)
            {
                parsed.Positionals.Add(token);
                continue;
            }

            if (string.Equals(token, "--", StringComparison.Ordinal))
            {
                terminated = true;
                continue;
            }

            if (!token.StartsWith("--", StringComparison.Ordinal))
            {
                parsed.Positionals.Add(token);
                continue;
            }

            var equalsIndex = token.IndexOf('=');
            var hasInlineValue = equalsIndex >= 0;
            var name = hasInlineValue ? token[..equalsIndex] : token;
            var isFlag = flags.Contains(name, StringComparer.Ordinal);
            var isSwitch = switches.Contains(name, StringComparer.Ordinal);

            if (!isFlag && !isSwitch)
            {
                throw new FailureException(2, $"unknown option {name}");
            }

            if (parsed.Options.ContainsKey(name))
            {
                throw new FailureException(2, $"option {name} given twice");
            }

            if (isSwitch)
            {
                if (hasInlineValue)
                {
                    throw new FailureException(2, $"option {name} takes no value");
                }

                parsed.Options.Add(name, string.Empty);
                continue;
            }

            if (hasInlineValue)
            {
                parsed.Options.Add(name, token[(equalsIndex + 1)..]);
                continue;
            }

            if (index + 1 >= args.Count || args[index + 1].StartsWith("--", StringComparison.Ordinal))
            {
                throw new FailureException(2, $"option {name} needs a value");
            }

            parsed.Options.Add(name, args[++index]);
        }

        return parsed;
    }
}
