using System.Text;
using Automapolis.TerminalShell;

Console.InputEncoding = Encoding.UTF8;
Console.OutputEncoding = new UTF8Encoding(false);

if (args is ["--help"] or ["-h"])
{
    Console.WriteLine(TerminalSession.Help);
    return 0;
}

if (args.Length > 0)
{
    Console.Error.WriteLine("Usage: TerminalShell [--help]");
    return 1;
}

var session = new TerminalSession(Console.Out);
session.Run(Console.In, interactive: !Console.IsInputRedirected && !Console.IsOutputRedirected);
return 0;
