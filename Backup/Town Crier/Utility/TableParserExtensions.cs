using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace TableParser
{
	//https://github.com/Robert-McGinley/TableParser
	public static class TableParserExtensions
	{
		static StringBuilder stringBuilder = new StringBuilder();

		public static string ToStringTable<T>(this IEnumerable<T> values, string[] columnHeaders, params Func<T, object>[] valueSelectors)
		{
			return ToStringTable(values.ToArray(), columnHeaders, valueSelectors);
		}

		public static string ToStringTable<T>(this T[] values, string[] columnHeaders, params Func<T, object>[] valueSelectors)
		{
			Debug.Assert(columnHeaders.Length == valueSelectors.Length);

			var arrValues = new string[values.Length + 1, valueSelectors.Length];

			// Fill headers
			for (int colIndex = 0; colIndex < arrValues.GetLength(1); colIndex++)
			{
				arrValues[0, colIndex] = columnHeaders[colIndex];
			}

			// Fill table rows
			for (int rowIndex = 1; rowIndex < arrValues.GetLength(0); rowIndex++)
			{
				for (int colIndex = 0; colIndex < arrValues.GetLength(1); colIndex++)
				{
					object value = valueSelectors[colIndex].Invoke(values[rowIndex - 1]);

					arrValues[rowIndex, colIndex] = value != null ? value.ToString() : "null";
				}
			}

			return ToStringTable(arrValues);
		}

		public static string ToStringTable(this string[,] arrValues)
		{
			int[] maxColumnsWidth = GetMaxColumnsWidth(arrValues);

			var headerSpliter = new string('-', maxColumnsWidth.Sum(i => i + 3) - 1);

			stringBuilder.Clear();

			stringBuilder.AppendLine();

			for (int rowIndex = 0; rowIndex < arrValues.GetLength(0); rowIndex++)
			{
				for (int colIndex = 0; colIndex < arrValues.GetLength(1); colIndex++)
				{
					// Print cell
					string cell = arrValues[rowIndex, colIndex];
					cell = cell.PadRight(maxColumnsWidth[colIndex]);
					stringBuilder.Append(" | ");
					stringBuilder.Append(cell);
				}

				// Print end of line
				stringBuilder.Append(" | ");
				stringBuilder.AppendLine();

				// Print splitter
				if (rowIndex == 0)
				{
					stringBuilder.AppendFormat(" |{0}| ", headerSpliter);
					stringBuilder.AppendLine();
				}
			}

			return stringBuilder.ToString();
		}

		private static int[] GetMaxColumnsWidth(string[,] arrValues)
		{
			var maxColumnsWidth = new int[arrValues.GetLength(1)];
			for (int colIndex = 0; colIndex < arrValues.GetLength(1); colIndex++)
			{
				for (int rowIndex = 0; rowIndex < arrValues.GetLength(0); rowIndex++)
				{
					int newLength = arrValues[rowIndex, colIndex].Length;
					int oldLength = maxColumnsWidth[colIndex];

					if (newLength > oldLength)
					{
						maxColumnsWidth[colIndex] = newLength;
					}
				}
			}

			return maxColumnsWidth;
		}

		public static string ToStringTable<T>(this IEnumerable<T> values, params Expression<Func<T, object>>[] valueSelectors)
		{
			var headers = valueSelectors.Select(func => GetProperty(func).Name).ToArray();
			var selectors = valueSelectors.Select(exp => exp.Compile()).ToArray();
			return ToStringTable(values, headers, selectors);
		}

		private static PropertyInfo GetProperty<T>(Expression<Func<T, object>> expression)
		{
			if (expression.Body is UnaryExpression)
			{
				if ((expression.Body as UnaryExpression).Operand is MemberExpression)
				{
					return ((expression.Body as UnaryExpression).Operand as MemberExpression).Member as PropertyInfo;
				}
			}

			if ((expression.Body is MemberExpression))
			{
				return (expression.Body as MemberExpression).Member as PropertyInfo;
			}
			return null;
		}
	}
}