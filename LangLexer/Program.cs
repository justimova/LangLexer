using System;

namespace LangLexer
{
	class Program
	{
		static void Main(string[] args)
		{
			LexAnalyzer lexAnalyzer = new LexAnalyzer();
			string source = "program prog\n" +
				"var\n" +
				"int a\n" +
				"float b\n" +
				"int c\n" +
				"begin\n" +
				"a := a + 5\n" +
				"b := 56.5\n" +
				"a := c ^ 5\n" +
				"do while a > 89 begin\n" +
				"a := (c / 45 ) * b\n" +
				"enddo\n" +
				"end";
			try
			{
				//Console.WriteLine(source);
				lexAnalyzer.Analysis(source);
				Console.WriteLine("Лексический анализатор отработал успешно");
				
				Console.WriteLine($"Количество лексем: {lexAnalyzer.TableOfSymbol.Count}");
				if (lexAnalyzer.TableOfSymbol.Count > 0)
				{
					Console.WriteLine("\nТаблица символов:\n");
					foreach (var lex in lexAnalyzer.TableOfSymbol)
					{
						Console.WriteLine($"{lex.NumLine}\t{lex.Lexema}\t{lex.Token}\t{lex.IdxIdConst}");
					}
				}
				if (lexAnalyzer.TableOfId.Count > 0)
				{
					Console.WriteLine("\nТаблица идентификаторов:\n");
					int index = 0;
					foreach (var id in lexAnalyzer.TableOfId)
					{
						Console.WriteLine($"{index++}\t{id}");
					}
				}
				if (lexAnalyzer.TableOfConst.Count > 0)
				{
					Console.WriteLine("\nТаблица констант:\n");
					int index = 0;
					foreach (var cnst in lexAnalyzer.TableOfConst)
					{
						Console.WriteLine($"{index++}\t{cnst}");
					}
				}
			}
			catch (LexAnalyzerException err)
			{
				Console.WriteLine($"Лексический анализатор отработал неуспешно.\n{err.Message}");
			}
			catch (Exception err)
			{
				Console.WriteLine($"Ошибка в работе лексического анализатора. Подробные сведения об ошибке:{err.Message}");
			}
		}
	}
}
