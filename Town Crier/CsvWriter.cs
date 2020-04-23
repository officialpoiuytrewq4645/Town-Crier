using System.Collections;
using System.IO;
using System.Text;

public static class CsvWriter
{
	public static void WriteCsvLine(this StreamWriter writer, params object[] parts)
	{
		writer.Write(parts[0]);

		for (int i = 1; i < parts.Length; i++)
		{
			writer.Write(',');

			IEnumerable enumerable = parts[i] as IEnumerable;

			if (enumerable != null && parts[i].GetType() != typeof(string))
			{
				WriteCsvLine(writer, enumerable);
			}
			else
			{
				writer.Write(parts[i]);
			}
		}

		writer.WriteLine();
	}

	public static void WriteCsvLine(this StreamWriter writer, IEnumerable parts, bool isNewLine = false)
	{
		bool isFirst = true;

		foreach (object part in parts)
		{
			if (!isFirst)
			{
				writer.Write(',');
			}

			isFirst = false;

			writer.Write(part);
		}

		if (isNewLine)
		{
			writer.WriteLine();
		}
	}

	public static void WriteCsvLine(this StringBuilder writer, params object[] parts)
	{
		writer.Append(parts[0]);

		for (int i = 1; i < parts.Length; i++)
		{
			writer.Append(',');
			writer.Append(parts[i]);
		}

		writer.AppendLine();
	}
}