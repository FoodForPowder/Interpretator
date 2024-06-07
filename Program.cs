using System;
using System.IO;



namespace IntepretatorCsharp
{

    public class Intepreter
    {
        const int BSIZE = 15;   // размер буфера для хранения идентификаторов
        const int NONE = -1;    // другой символ обычно ;
        const char EOS = '\0';  // конец строки
        const int NUM = 256;    // число
        const int ID = 257;     // переменная
        const int ARRAY = 258;  // массив     
        const int READ = 264;
        const int WRITE = 265;
        const int INT = 266;    // целочисленный тип переменной используется при объявлении
        const int INTM = 267;   // целочисленный тип массива используется при объявлении
        const int INDEX = 268;  // метка массива для ОПС
        const int DONE = 271;   // конец программы

        const int STRMAX = 999; // Размер массива лексем
        const int SYMMAX = 10000000; // Размер таблицы символов
        public bool GlobError = false;
        const int STACKSIZE = 500000;
        int massM = 0;
        int intM = 0;

 
        // Таблица лексического анализатора, номера семантических программ
        int[,] LexTable = {
         //   б   ц  " "  =   /   *   +   -  [    ]   (    )   ;   др  @   ,  
            { 0,  2, 18,  13,  6,  7,  8,  9, 20, 17, 10,  11, 26, 19,14, 29 }, //начальное состояние
            { 1,  1, 15,  15, 15, 15, 15, 15, 15, 15, 15, 15, 15,  19, 15, 15 }, // состояние чтения буквы
            { 21, 3, 16,  23, 16, 16, 16, 16, 23, 16, 23, 16, 16,  19, 16, 23 } //состояние чтения цифры
        };
        // Таблица ключевых слов
        TableEntry[] Keywords = {           
            new TableEntry { Lexeme = "read", Token = READ },
            new TableEntry { Lexeme = "write", Token = WRITE },
            new TableEntry { Lexeme = "integer", Token = INT },
            new TableEntry { Lexeme = "mass", Token = INTM },
            new TableEntry { Lexeme = "0", Token = 0 },
        };
        /// <summary>
        /// // Таблица лексем
        /// </summary>
        public TableEntry[] SymbolsTable = new TableEntry[SYMMAX];
        /// <summary>
        ///  Переменные и их значения
        /// </summary>
        public TableEntry[] Variables = new TableEntry[SYMMAX];    

        // Массивы и их содержимое
        public MassTableEntry[] MassesTable = new MassTableEntry[SYMMAX]; // хранит массивы во время выполнения OPSStack
        public MassElement[] Passport = new MassElement[SYMMAX]; // помогает обращаться к массивам
        /// <summary>
        /// Генерируемая ОПС
        /// </summary>
        public OPSElement[] OPSMass = new OPSElement[9999];
        /// <summary>
        /// // Счетчик элементов в ОПС
        /// </summary>
        public int OPSCounter = 0; // Счетчик элементов в ОПС
        /// <summary>
        /// // Последняя использованная позиция в (SymbolsTable)systemtablе Lexemes нужно для проверок переполнения массива лексем
        /// </summary>
        public int LastChar = -1;  // Последняя использованная позиция в (SymbolsTable)systemtablе Lexemes нужно для проверок переполнения массива лексем
        /// <summary>
        /// // Последняя использованная позиция в таблице символов нужно для проверок
            ///  переполнения символьной таблицы и адресации массивов:переменных и массивов 
        /// </summary>
        public int LastEntry = 0;  // Последняя использованная позиция в таблице символов нужно для проверок
                                   // переполнения символьной таблицы и адресации массивов:переменных и массивов 
        /// <summary>
        /// // очередная лексемма
        /// </summary>
        public int LookAhead;      

        /// <summary>
        /// Текст программы
        /// </summary>
        public char[] ProgramText; 
        /// <summary>
        /// // Номер обозреваемого символа
        /// </summary>
        int k = -1;     // Номер обозреваемого символа
        /// <summary>
        ///  // Номер символа
        /// </summary>
        int ss = 1;    // Номер символа
        /// <summary>
        /// //номер семантической программы было перед лексическим анализатором
        /// </summary>
        int semanticProgram;         //номер семантической программы было перед лексическим анализатором

        /// <summary>
        /// // Буфер Лексемы в лексическом анализаторе в нем храниться распознаваемая лаксема
        /// </summary>
        char[] LexBuffer = new char[BSIZE]; // Буфер Лексемы в лексическом анализаторе в нем храниться распознаваемая лаксема
        /// <summary>
        /// // Номер строки
        /// </summary>
        int LineNumber = 1; // Номер строки
        /// <summary>
        /// значение токена ОПС
        /// </summary>
        int TokenValue = NONE;
        /// <summary>
        /// // Символьная длина элемента в ОПС
        /// </summary>
        int x = 10; // Символьная длина элемента в ОПС

        /// <summary>
        /// // Типы элементов в ОПС
        /// </summary>
        public enum OPSType
        {   /// <summary>
        /// переменная
        /// </summary>
            IDE,
            /// <summary>
            /// массив
            /// </summary>
            MAS,   // переменная или массив
            /// <summary>
            /// число
            /// </summary>
            NUMBER,     // число
            /// <summary>
            /// операция
            /// </summary>
            SIGN,       // операция
            /// <summary>
            /// индексатор
            /// </summary>
            IND,        // индексатор          
            /// <summary>
            /// чтение
            /// </summary>
            RE,
            /// <summary>
            /// запись
            /// </summary>
            WR          // метка записи
        };

        /// <summary>
        /// // Элементы ОПС
        /// </summary>
        public struct OPSElement
        {
            /// <summary>
            /// Символ эллемента
            /// </summary>
            public char[] Element { get; set; }
            /// <summary>
            /// тип
            /// </summary>
            public OPSType Type { get; set; }
        }

        /// <summary>
        /// // Запись в таблице символов и таблице переменных
        /// </summary>
        public struct TableEntry
        {
            public string Lexeme;
            public int Token;
        }
        /// <summary>
        /// // хранит массивы во время выполнения OPSStack
        /// </summary>
        public struct MassTableEntry
        {
            public char[] Mass;
            public int[] Element;
        }
        /// <summary>
        /// // помогает обращаться к массивам
        /// </summary>
        public struct MassElement
        {
            public int Mass;
            public int Element;
        }
        /// <summary>
        /// эллемент стека
        /// </summary>
        public struct StackElement
        {
            public int Value;
            public OPSType Type;
        }
        /// <summary>
        /// Cтек
        /// </summary>
        public class Stack
        {

            public StackElement[] StackElements { get; set; }
            private int ElementCounter { get; set; }

            /// <summary>
            /// инициализация стека
            /// </summary>
            public Stack()
            {

                StackElements = new StackElement[STACKSIZE];
                ElementCounter = 0;
                Console.WriteLine("Stack Initialized");
            }

            /// <summary>
            /// добавленив в стек
            /// </summary>
            /// <param name="element"></param>
            public void Push(StackElement element)
            {
                if (ElementCounter == STACKSIZE)
                {
                    Console.WriteLine("Stack is full");
                    return;
                }

                StackElements[ElementCounter] = element;
                ElementCounter++;
            }

            /// <summary>
            /// удаление из стека
            /// </summary>
            /// <returns></returns>
            public StackElement Pop()
            {
                if (ElementCounter == 0)
                {
                    Console.WriteLine("Stack is empty");
                }

                ElementCounter--;
                return StackElements[ElementCounter];
            }
        }
        /// <summary>
        /// // Загрузка ключевых слов в таблицу символов
        /// </summary>

        public void Initialize()
        {
            TableEntry[] entries = Keywords;
            foreach (var entry in entries)
            {
                Insert(entry.Lexeme, entry.Token);
            }
        }
        /// <summary>
        ///  Возвращает положение в таблице лексем для s
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>

        public int Lookup(string s)
        {
            for (int i = LastEntry; i > 0; i--)
            {
                if (s.CompareTo(SymbolsTable[i].Lexeme) == 0)
                {
                    return i;
                }
            }
            return 0;
        }

        /// <summary>
        /// // Добавляет новую лексему и возвращает положение в таблице лексем для s
        /// </summary>
        /// <param name="s"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public int Insert(string s, int token)
        {
            int len = s.Length;

            bool massOverflow = LastEntry + 1 >= SYMMAX;
            if (massOverflow)
            {
                Console.WriteLine("Symbol Table is full");
                Environment.Exit(1);
            }

            bool lexMassOverflow = LastChar + len + 1 >= STRMAX;
            if (lexMassOverflow)
            {
                Console.WriteLine("Lexemes Array is full");
                Environment.Exit(1); ;
            }

            LastEntry++;                                // Переходим к следующей строке в таблице лексем
            SymbolsTable[LastEntry].Token = token;      // Устанавливаем хранимый токен 
            SymbolsTable[LastEntry].Lexeme = s;         // Адрес начала лексемы в таблице лексем

            if (token == ID)
            {
                Variables[LastEntry].Lexeme = s;
            }

            if (token == ARRAY)
            {
                MassesTable[LastEntry].Mass = s.ToCharArray();
            }

            LastChar = LastChar + len + 1;         // Обновляем последнюю позицию в массиве лексем
            SymbolsTable[LastEntry].Lexeme = s;    // Заполняем таблицу лексем

            return LastEntry;
        }

        /// <summary>
        /// Взятие очередного символа текста
        /// </summary>
        /// <returns></returns>
        public char GetSymbol()
        {
            // k Номер обозреваемого символа
            // ss Номер символа
            ss++; k++;
            return ProgramText[k];
        }

        //////////////////////////////////////////////////////  Лексический анализатор \\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\
        public int LexicalAnalyzer()
        {
            //state
            // 0 - начальное
            // 1, 2 - распознаем имя, распознаем константу
            // 3 - конечное

            int b = 0; //символ лексемы 
                int symbolFromLookup;
            char readSymbol; // очередной символ
            int state = 0; // состояние
            bool error = false;
            //int help = 0;

            while ((!error) || (state < 3))
            {
                readSymbol = GetSymbol();
                //help = readSymbol;
                semanticProgram = LexTable[state, GetProgramm(readSymbol) - 1]; //получаем номер семаннтической программы, которую нужно исполнить

                //if (help == 13) LineNumber--;
                //Console.WriteLine(ss + " " + LineNumber);

                switch (semanticProgram)
                {
                    //Начало идентификатора / служ. слова
                    case 0:
                        state = 1;
                        LexBuffer[b] = readSymbol;
                        b++;
                        break;

                    //Продолжение идентификатора / служ. слова
                    case 1:
                        state = 1;
                        LexBuffer[b] = readSymbol;
                        b++;
                        break;

                    //Начало числа
                    case 2:
                        state = 2;
                        TokenValue = (int)Char.GetNumericValue(readSymbol);
                        break;

                    //Продолжение числа
                    case 3:
                        state = 2;
                        TokenValue = TokenValue * 10 + (int)Char.GetNumericValue(readSymbol);
                        break;


                    
                    case 6: //Распознано деление
                    
                    case 7: //Распознано умножение
                    
                    case 8://Распознано сложение
                    
                    case 9://Распознано сложение
                    
                    case 10://Распознана (
                    
                    case 11: // Распознана )
                    
                    case 13://Распознано присваивание
                    
                    case 17://  ]
                    
                    case 20:// [
                    
                    case 26://  ;                  
                    
                    case 29: // ,
                        state = 3;
                        TokenValue = NONE;
                        return readSymbol;

                    
                    case 14: //Конец файла
                        state = 3;
                        return DONE;

                    
                    case 15: // Поиск идентификатора в таблице  служ. слов
                        state = 3;
                        LexBuffer[b] = EOS;
                        if (readSymbol != '@')
                        {
                            k--;
                            ss--;
                        }

                        string name = new string(LexBuffer);
                        string nameInt = "integer";
                        string nameMass = "mass";
                        if (name.Contains(nameMass))
                        {
                            massM = 1;
                            intM = 0;
                        }

                        if (name.Contains(nameInt))
                        {
                            intM = 1;
                            massM = 0;                           
                        }

                        // Console.WriteLine(name);

                        //поиск слова в таблице символов
                        symbolFromLookup = Lookup(new string(LexBuffer));

                        if (symbolFromLookup == 0)
                        {
                            if (massM == 1)
                                symbolFromLookup = Insert(new string(LexBuffer), ARRAY);
                            else if (intM == 1)
                                symbolFromLookup = Insert(new string(LexBuffer), ID);
                        }

                        TokenValue = symbolFromLookup;
                        Array.Clear(LexBuffer, 0, LexBuffer.Length);
                        return SymbolsTable[symbolFromLookup].Token; //значение добавленного идентификатора

                    
                    case 16: // Распознано число
                        state = 3;
                        k--;
                        ss--;
                        return NUM;

                    
                    case 18: //Распознан пробел
                        state = 0;
                        break;

                    
                    case 19://Распознанан символ, не относящийся к языку
                        state = 4;
                        error = true;
                        Console.WriteLine("Unknown symbol");
                        TokenValue = NONE;
                        return readSymbol;

                    
                    case 21://Ошибка в лексеме
                        state = 4;
                        Console.WriteLine("Wrong lexem");
                        error = true;
                        break;
                                       
                    case 23://Другая ошибка
                        state = 4;
                        Console.WriteLine("Wrong expression");
                        error = true;
                        break;
                   
                }

            }

            return 0;
        }
        /// <summary>
        /// получаем номер стобца в таблице переходов
        /// </summary>
        /// <param name="ch"></param>
        /// <returns></returns>
        public int GetProgramm(char ch)
        {
            if ((ch >= 'A' && ch <= 'Z') || (ch >= 'a' && ch <= 'z'))
                return 1;
            else if (char.IsDigit(ch))
                return 2;
            else if (ch == ' ')
                return 3;
            else if (ch == '=')
                return 4;
            else if (ch == '/')
                return 5;
            else if (ch == '*')
                return 6;
            else if (ch == '+')
                return 7;
            else if (ch == '-')
                return 8;
            else if (ch == '[')
                return 9;
            else if (ch == ']')
                return 10;
            else if (ch == '(')
                return 11;
            else if (ch == ')')
                return 12;
            else if (ch == ';')
                return 13;                        
            else if (ch == '@')
                return 15;
            else if (ch == ',')
                return 16;
            else
                return 14; // Для всех остальных символов

        }
        ////////////////////////////////////////////////////  Сохранение ОПС   \\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\
        /// <summary>
        /// Сохранение ОПС (генератор ОПС)
        /// </summary>
        /// <param name="t">Токен ОПС</param>
        /// <param name="tval">Значение токена ОПС</param>
        public void Emit(int t, int tval)
        {
            OPSMass[OPSCounter].Element = new char[x];
            switch (t)
            {
                case '+':
                    OPSMass[OPSCounter].Element = "+".ToCharArray();
                    OPSMass[OPSCounter].Type = OPSType.SIGN;
                    OPSCounter++;
                    Console.WriteLine((char)t);
                    break;
                case '-':
                    OPSMass[OPSCounter].Element = "-".ToCharArray();
                    OPSMass[OPSCounter].Type = OPSType.SIGN;
                    OPSCounter++;
                     Console.WriteLine((char)t);
                    break;
                case '*':
                    OPSMass[OPSCounter].Element = "*".ToCharArray();
                    OPSMass[OPSCounter].Type = OPSType.SIGN;
                    OPSCounter++;
                    Console.WriteLine((char)t);
                    break;
                case '/':
                    OPSMass[OPSCounter].Element = "/".ToCharArray();
                    OPSMass[OPSCounter].Type = OPSType.SIGN;
                    OPSCounter++;
                    Console.WriteLine((char)t);
                    break;
                case '=':
                    OPSMass[OPSCounter].Element = "=".ToCharArray();
                    OPSMass[OPSCounter].Type = OPSType.SIGN;
                    OPSCounter++;
                    Console.WriteLine((char)t);
                    break;               
                case NUM:
                    OPSMass[OPSCounter].Element = tval.ToString().ToCharArray();
                    OPSMass[OPSCounter].Type = OPSType.NUMBER;
                    OPSCounter++;
                    Console.WriteLine(tval);
                    break;
                case ARRAY:
                    OPSMass[OPSCounter].Element = SymbolsTable[tval].Lexeme.ToCharArray();
                    OPSMass[OPSCounter].Type = OPSType.MAS;
                    OPSCounter++;
                    Console.WriteLine(SymbolsTable[tval].Lexeme);
                    break;
                case ID:
                    OPSMass[OPSCounter].Element = SymbolsTable[tval].Lexeme.ToCharArray();
                    OPSMass[OPSCounter].Type = OPSType.IDE;
                    OPSCounter++;
                    Console.WriteLine(SymbolsTable[tval].Lexeme);
                    break;
                case INDEX:
                    OPSMass[OPSCounter].Element = "<i>".ToCharArray();
                    OPSMass[OPSCounter].Type = OPSType.IND;
                    OPSCounter++;
                    Console.WriteLine("<i>");
                    break;               
                case READ:
                    OPSMass[OPSCounter].Element = "<r>".ToCharArray();
                    OPSMass[OPSCounter].Type = OPSType.RE;
                    OPSCounter++;
                    Console.WriteLine("<r>");
                    break;
                case WRITE:
                    OPSMass[OPSCounter].Element = "<w>".ToCharArray();
                    OPSMass[OPSCounter].Type = OPSType.WR;
                    OPSCounter++;
                    Console.WriteLine("<w>");
                    break;
                default:
                    Console.WriteLine();
                    break;

            }
        }

        // Переход к следующей лексеме 
        int[] Table = 
            {
            0, 1, 2, 3, 4, 5, 6,
            7, 8, 9, 10, 11, 12
        };

        string[] TableSt = {
            "NUMBER", "ID", "ARRAY", "", "", "", "", 
            "", "READ", "WRITE", "INTEGER", "MASS", "NAMEMASS"
        };

        ////////////// Сопоставление входного значения, классифицируем, сопоставляем и передаем анализатору или выкиждываем ошибку
        /// <summary>
        /// Сопоставление входного значения, классифицируем, сопоставляем и передаем анализатору или выкиждываем ошибку
        /// </summary>
        /// <param name="t"></param>
        public void Match(int t)
        {

            if (GlobError != true)
            {
                char p, pp; //символ //лексема
                string help1 = "", help2 = "";
                if (t < 128)
                {
                    p = Convert.ToChar(t);
                    help1 += p;
                    if (LookAhead < 128)
                    {

                        pp = Convert.ToChar(LookAhead);

                        help2 += pp;
                    }
                    else help2 = TableSt[Table[LookAhead - 256]];
                }

                else
                {
                    help1 = TableSt[Table[t - 256]];
                    if (LookAhead < 128)
                    {

                        pp = Convert.ToChar(LookAhead);

                        help2 += pp;
                    }
                    else
                    {

                        help2 = TableSt[Table[LookAhead - 256]];
                    }
                }

                if (LookAhead == t)
                    LookAhead = LexicalAnalyzer();
                else
                {
                    GlobError = true;
                    Console.WriteLine("ERROR bad symbol: " + "Line: " + LineNumber + "  Symbol: " + ss + "  Expected: " + help1 + "  Now: " + help2);
                }
            }
        }

        /// <summary>
        /// Выделение памяти для хранения массивов
        /// </summary>
        public void SetSize()
        {
            if (LookAhead == NUM)
                MassesTable[LastEntry].Element = new int[TokenValue];
        }  
        /// <summary>
        /// Содержание умножения и деления правило F
        /// </summary>
        public void MultiplyOrDevideContent()   // F
        {
            if (GlobError != true)
            {
                switch (LookAhead)
                {
                    case NUM:
                        Emit(NUM, TokenValue);
                        Match(NUM);
                        break;

                    case ID:
                        Emit(ID, TokenValue);
                        Match(ID);
                        break;

                    case ARRAY:
                        Emit(ARRAY, TokenValue);
                        Match(ARRAY);
                        Match('[');
                        Expression();
                        Match(']');
                        Emit(INDEX, 0);
                        break;

                    case '(':
                        Match('(');
                        Expression();
                        Match(')');
                        break;

                    default:

                        Console.WriteLine("Error multuply or divide content: " + "Line: " + LineNumber + "  Symbol: " + ss);
                        GlobError = true;
                        break;
                }
            }
        }

        /// <summary>
        /// Анализ умножения и деления правило V
        /// </summary>
        public void MultiplyOrDivide()  
        {
            if (GlobError != true)
            {
                int t;
                switch (LookAhead)
                {

                    case '*':
                    case '/':
                        t = LookAhead;
                        Match(LookAhead);
                        MultiplyOrDevideContent();
                        Emit(t, NONE);
                        MultiplyOrDivide();
                        break;

                    case ')':
                    case ']':
                    case ';':                   
                    case '+':
                    case '-':
                    case DONE:
                        break;

                    default:
                        Console.WriteLine("Error operation sign was expected: " + "Line: " + LineNumber + "  Symbol: " + ss);
                        GlobError = true;
                        break;
                }
            }
        }
        /// <summary>
        /// Содержание сложения и вычитания правило T
        /// </summary>
        public void PlusOrMinusContent()   
        {
            if (GlobError != true)
            {
                switch (LookAhead)
                {
                    case NUM:
                        Emit(NUM, TokenValue);
                        Match(NUM);
                        MultiplyOrDivide();//V
                        break;

                    case ID:
                        Emit(ID, TokenValue);
                        Match(ID);
                        MultiplyOrDivide();//V
                        break;

                    case ARRAY:
                        Emit(ARRAY, TokenValue);
                        Match(ARRAY);
                        Match('[');
                        Expression();
                        Match(']');
                        Emit(INDEX, 0);
                        MultiplyOrDivide();
                        break;

                    case '(':
                        Match('(');
                        Expression();
                        Match(')');
                        MultiplyOrDivide();
                        break;

                    default:
                        Console.WriteLine("Error plus or minus content: " + "Line: " + LineNumber + "  Symbol: " + ss);
                        GlobError = true;

                        break;
                }

            }
        }

        /// <summary>
        /// // Анализ сложения и вычитания правило U
        /// </summary>
        public void PlusOrMinus() 
        {
            if (GlobError != true)
            {
                int t;

                switch (LookAhead)
                {
                    case '+':
                    case '-':
                        t = LookAhead;
                        Match(t);
                        PlusOrMinusContent(); //T
                        Emit(t, NONE);
                        PlusOrMinus();//U
                        break;
                    case ')':
                    case ']':
                    case ';':                   
                    case DONE:
                        break;

                    default:
                        Console.WriteLine("Error operation sign was expected: " + "Line: " + LineNumber + "  Symbol: " + ss);
                        GlobError = true;
                        break;
                }
            }
        }

        /// <summary>
        /// // Анализ выражений правило S
        /// </summary>
        public void Expression()    
        {
            if (GlobError != true)
            {
                switch (LookAhead)
                {
                    case NUM: //k
                        Emit(NUM, TokenValue);
                        Match(NUM);
                        MultiplyOrDivide(); //V
                        PlusOrMinus(); //U
                        break;

                    case ID:
                        Emit(ID, TokenValue);
                        Match(ID);
                        MultiplyOrDivide();//V
                        PlusOrMinus();//U
                        break;

                    case ARRAY:
                        Emit(ARRAY, TokenValue);
                        Match(ARRAY);
                        Match('[');
                        Expression();
                        Match(']');
                        Emit(INDEX, 0);
                        MultiplyOrDivide();//v
                        PlusOrMinus(); //u
                        break;

                    case '(':
                        Match('(');
                        Expression();
                        Match(')');
                        MultiplyOrDivide();
                        PlusOrMinus();
                        break;



                    default:
                        Console.WriteLine("Error expression content: " + "Line: " + LineNumber + "  Symbol: " + ss);
                        GlobError = true;
                        break;
                }

            }
        }
        /// <summary>
        /// Множественное объявление правило X
        /// </summary>
        public void SeveralNamesID() 
        {
            if (GlobError != true)
            {
                switch (LookAhead)
                {

                    case ',':

                        Match(',');
                        Match(ID);
                        SeveralNamesID();
                        break;

                    case ';':
                        intM = 0; massM = 0;
                        Match(';');

                        break;

                    default:
                        Console.WriteLine("Error in description integer variables: " + "Line: " + LineNumber + "  Symbol: " + ss);
                        GlobError = true;
                        break;
                }

            }
        }


        public void SeveralNamesMASS() // W
        {
            if (GlobError != true)
            {
                switch (LookAhead)
                {

                    case ',':

                        Match(',');
                        Match(ARRAY);
                        Match('[');
                        SetSize();
                        Match(NUM);
                        Match(']');
                        SeveralNamesMASS();

                        break;

                    case ';':
                        intM = 0; massM = 0;
                        Match(';');

                        break;

                    default:
                        Console.WriteLine("Error in description array variables: " + "Line: " + LineNumber + "  Symbol: " + ss);
                        GlobError = true;
                        break;
                }
            }
        }


        /// <summary>
        /// Анализ переменных и массивов в выражениях правило L или M
        /// </summary>
        public void Name()
        {
            if (GlobError != true)
            {
                switch (LookAhead)
                {

                    case ID:                        //L                  
                        Match(ID);
                        SeveralNamesID();
                        break;

                    case ARRAY:                     //M               
                        Match(ARRAY);
                        Match('[');
                        SetSize();
                        Match(NUM);
                        Match(']');
                        SeveralNamesMASS();
                        break;

                    default:
                        Console.WriteLine("Error in variables definition : " + "Line: " + LineNumber + "  Symbol: " + ss);
                        GlobError = true;
                        break;
                }

            }
        }

        /// <summary>
        /// Анализ Ввода и вывода правило A
        /// </summary>
        public void MegaExpression() 
        {

            if (GlobError != true)
            {                
                switch (LookAhead)
                {

                    case ID:
                        Emit(ID, TokenValue);
                        Match(ID);
                        Match('=');
                        Expression();
                        Emit('=', NONE);
                        Match(';');
                        MegaExpression();
                        break;

                    case ARRAY:
                        Emit(ARRAY, TokenValue);
                        Match(ARRAY);
                        Match('[');
                        Expression();
                        Match(']');
                        Emit(INDEX, 0);
                        Match('=');
                        Expression();
                        Emit('=', NONE);
                        Match(';');
                        MegaExpression();
                        break;                                   
                    case READ:
                        Match(READ);
                        Match('(');
                        switch (LookAhead)
                        {

                            case ID:
                                Emit(ID, TokenValue);
                                Match(ID);
                                break;

                            case ARRAY:
                                Emit(ARRAY, TokenValue);
                                Match(ARRAY);
                                Match('[');
                                Expression();
                                Match(']');
                                Emit(INDEX, 0);
                                break;

                            default:
                                Console.WriteLine("Error read operator: " + "Line: " + LineNumber + "  Symbol: " + ss);
                                GlobError = true;
                                break;
                        }

                        Match(')');
                        Emit(READ, 0);
                        Match(';');
                        MegaExpression();
                        break;

                    case WRITE:
                        Match(WRITE);
                        Match('(');
                        Expression();
                        Match(')');
                        Emit(WRITE, 0);
                        Match(';');
                        MegaExpression();
                        break;                    
                    case DONE:
                        break;

                    default:
                        Console.WriteLine("Error program content: " + "Line: " + LineNumber + "  Symbol: " + ss);
                        GlobError = true;
                        break;

                }
            }
        }

        ////////////////////////////   Начало синтаксического анализатора анализ объявления переменных  \\\\\\\\\\\\\\\\\\\\\\\\\\\\
        /// <summary>
        /// Начало синтаксического анализатора анализ объявления переменных правило P
        /// </summary>
        public void InitializeNames() 
        {
            if (GlobError != true)
            {
                if (LookAhead != DONE)
                {
                    switch (LookAhead)
                    {
                        case INT:
                            Match(INT);
                            Name(); // L                        
                            InitializeNames();
                            break;
                        case INTM:
                            Match(INTM);
                            Name();  // M                         
                            InitializeNames();
                            break;
                        default:
                            MegaExpression();
                            break;
                    }
                }
                return;
            }
        }
///////////////////// Выполнение программы
/// <summary>
/// Выполнение программыo(интерпритатор)
/// </summary>
        public void OPSStack()
        {
            int length = OPSCounter;
            int z = 0, d = 0, res = 0, ps = 0;
            char t;

            Stack stack = new Stack();
            StackElement firstElement;
            StackElement secondElement;

            while (z < length)
            {
                switch (OPSMass[z].Type)
                {
                    case OPSType.IDE:
                        d = Lookup(new string(OPSMass[z].Element)); // Позиция в таблице переменных 
                        firstElement.Value = d;         // Позиция
                        firstElement.Type = OPSType.IDE; // Тип
                        stack.Push(firstElement);       // Помещаем в стек
                        z++;                            // Переходим к следующему элементу
                        break;

                    case OPSType.NUMBER:
                        firstElement.Value = Convert.ToInt32(new string(OPSMass[z].Element));
                        firstElement.Type = OPSType.NUMBER;
                        stack.Push(firstElement); // Помещаем в стек
                        z++;
                        break;

                    case OPSType.MAS:
                        d = Lookup(new string(OPSMass[z].Element)); // Позиция в таблице массивов
                        firstElement.Value = d; // Позиция
                        firstElement.Type = OPSType.MAS;
                        stack.Push(firstElement);
                        z++; // Переходим к следующему элементу
                        break;

                    case OPSType.IND:
                        firstElement = stack.Pop(); // Индекс элемента
                        secondElement = stack.Pop(); // Массив

                        switch (firstElement.Type)
                        {
                            case OPSType.IDE:
                                Passport[ps].Element = Variables[firstElement.Value].Token;
                                break;
                            case OPSType.NUMBER:
                                Passport[ps].Element = firstElement.Value;
                                break;
                            default:
                                break;
                        }

                        Passport[ps].Mass = secondElement.Value;
                        firstElement.Value = ps; ps++;
                        firstElement.Type = OPSType.MAS;
                        stack.Push(firstElement); z++;
                        break;

                    case OPSType.SIGN:
                        t = OPSMass[z].Element[0];
                        switch (t)
                        {
                            case '=':
                                firstElement = stack.Pop(); // Правое значение
                                secondElement = stack.Pop(); // Левое значение

                                switch (secondElement.Type)
                                {
                                    case OPSType.IDE:
                                        switch (firstElement.Type)
                                        {
                                            case OPSType.IDE:
                                                Variables[secondElement.Value].Token = Variables[firstElement.Value].Token;
                                                break;

                                            case OPSType.MAS:
                                                Variables[secondElement.Value].Token = MassesTable[Passport[firstElement.Value].Mass].Element[Passport[firstElement.Value].Element];
                                                break;

                                            case OPSType.NUMBER:
                                                Variables[secondElement.Value].Token = firstElement.Value;
                                                break;

                                            default:
                                                break;
                                        }
                                        break;

                                    case OPSType.MAS:
                                        switch (firstElement.Type)
                                        {
                                            case OPSType.IDE:
                                                MassesTable[Passport[secondElement.Value].Mass].Element[Passport[secondElement.Value].Element] = Variables[firstElement.Value].Token;
                                                break;

                                            case OPSType.MAS:
                                                MassesTable[Passport[secondElement.Value].Mass].Element[Passport[secondElement.Value].Element]
                                                    = MassesTable[Passport[firstElement.Value].Mass].Element[Passport[firstElement.Value].Element];
                                                break;

                                            case OPSType.NUMBER:
                                                MassesTable[Passport[secondElement.Value].Mass].Element[Passport[secondElement.Value].Element] = firstElement.Value;
                                                break;

                                            default:
                                                break;
                                        }
                                        break;

                                    case OPSType.NUMBER:
                                        switch (firstElement.Type)
                                        {
                                            case OPSType.IDE:
                                                res = secondElement.Value - Variables[firstElement.Value].Token;
                                                break;

                                            case OPSType.NUMBER:
                                                res = secondElement.Value - firstElement.Value;
                                                break;

                                            case OPSType.MAS:
                                                res = MassesTable[Passport[firstElement.Value].Mass].Element[Passport[firstElement.Value].Element] - secondElement.Value;
                                                break;

                                            default:
                                                break;
                                        }
                                        break;
                                }

                                firstElement.Value = res;
                                firstElement.Type = OPSType.NUMBER;
                                stack.Push(firstElement);
                                z++;
                                break;


                            case '*':
                                firstElement = stack.Pop();     // Правое значение
                                secondElement = stack.Pop();    // Левое значение

                                switch (secondElement.Type)
                                {
                                    case OPSType.IDE:
                                        switch (firstElement.Type)
                                        {
                                            case OPSType.IDE:
                                                res = Variables[secondElement.Value].Token * Variables[firstElement.Value].Token;
                                                break;

                                            case OPSType.MAS:
                                                res = Variables[secondElement.Value].Token * MassesTable[Passport[firstElement.Value].Mass].Element[Passport[firstElement.Value].Element];
                                                break;

                                            case OPSType.NUMBER:
                                                res = Variables[secondElement.Value].Token * firstElement.Value;
                                                break;

                                            default:
                                                break;
                                        }
                                        break;

                                    case OPSType.MAS:
                                        switch (firstElement.Type)
                                        {
                                            case OPSType.IDE:
                                                res = MassesTable[Passport[secondElement.Value].Mass].Element[Passport[secondElement.Value].Element] * Variables[firstElement.Value].Token;
                                                break;

                                            case OPSType.MAS:
                                                res = MassesTable[Passport[secondElement.Value].Mass].Element[Passport[secondElement.Value].Element]
                                                    * MassesTable[Passport[firstElement.Value].Mass].Element[Passport[firstElement.Value].Element];
                                                break;

                                            case OPSType.NUMBER:
                                                res = MassesTable[Passport[secondElement.Value].Mass].Element[Passport[secondElement.Value].Element] * firstElement.Value;
                                                break;

                                            default:
                                                break;
                                        }
                                        break;

                                    case OPSType.NUMBER:
                                        switch (firstElement.Type)
                                        {
                                            case OPSType.IDE:
                                                res = secondElement.Value * Variables[firstElement.Value].Token;
                                                break;

                                            case OPSType.NUMBER:
                                                res = secondElement.Value * firstElement.Value;
                                                break;

                                            case OPSType.MAS:
                                                res = MassesTable[Passport[firstElement.Value].Mass].Element[Passport[firstElement.Value].Element] * secondElement.Value;
                                                break;

                                            default:
                                                break;
                                        }
                                        break;
                                }

                                firstElement.Value = res;
                                firstElement.Type = OPSType.NUMBER;
                                stack.Push(firstElement);
                                z++;
                                break;

                            case '+':
                                firstElement = stack.Pop();     // Правое значение
                                secondElement = stack.Pop();    // Левое значение

                                switch (secondElement.Type)
                                {
                                    case OPSType.IDE:
                                        switch (firstElement.Type)
                                        {
                                            case OPSType.IDE:
                                                res = Variables[secondElement.Value].Token + Variables[firstElement.Value].Token;
                                                break;

                                            case OPSType.MAS:
                                                res = Variables[secondElement.Value].Token + MassesTable[Passport[firstElement.Value].Mass].Element[Passport[firstElement.Value].Element];
                                                break;

                                            case OPSType.NUMBER:
                                                res = Variables[secondElement.Value].Token + firstElement.Value;
                                                break;

                                            default:
                                                break;
                                        }
                                        break;

                                    case OPSType.MAS:
                                        switch (firstElement.Type)
                                        {
                                            case OPSType.IDE:
                                                res = MassesTable[Passport[secondElement.Value].Mass].Element[Passport[secondElement.Value].Element] + Variables[firstElement.Value].Token;
                                                break;

                                            case OPSType.MAS:
                                                res = MassesTable[Passport[secondElement.Value].Mass].Element[Passport[secondElement.Value].Element]
                                                    + MassesTable[Passport[firstElement.Value].Mass].Element[Passport[firstElement.Value].Element];
                                                break;

                                            case OPSType.NUMBER:
                                                res = MassesTable[Passport[secondElement.Value].Mass].Element[Passport[secondElement.Value].Element] + firstElement.Value;
                                                break;

                                            default:
                                                break;
                                        }
                                        break;

                                    case OPSType.NUMBER:
                                        switch (firstElement.Type)
                                        {
                                            case OPSType.IDE:
                                                res = secondElement.Value + Variables[firstElement.Value].Token;
                                                break;

                                            case OPSType.NUMBER:
                                                res = secondElement.Value + firstElement.Value;
                                                break;

                                            case OPSType.MAS:
                                                res = MassesTable[Passport[firstElement.Value].Mass].Element[Passport[firstElement.Value].Element] + secondElement.Value;
                                                break;

                                            default:
                                                break;
                                        }
                                        break;
                                }

                                firstElement.Value = res;
                                firstElement.Type = OPSType.NUMBER;
                                stack.Push(firstElement);
                                z++;
                                break;

                            case '-':
                                firstElement = stack.Pop();     // Правое значение
                                secondElement = stack.Pop();    // Левое значение

                                switch (secondElement.Type)
                                {
                                    case OPSType.IDE:
                                        switch (firstElement.Type)
                                        {
                                            case OPSType.IDE:
                                                res = Variables[secondElement.Value].Token - Variables[firstElement.Value].Token;
                                                break;

                                            case OPSType.MAS:
                                                res = Variables[secondElement.Value].Token - MassesTable[Passport[firstElement.Value].Mass].Element[Passport[firstElement.Value].Element];
                                                break;

                                            case OPSType.NUMBER:
                                                res = Variables[secondElement.Value].Token - firstElement.Value;
                                                break;

                                            default:
                                                break;
                                        }
                                        break;

                                    case OPSType.MAS:
                                        switch (firstElement.Type)
                                        {
                                            case OPSType.IDE:
                                                res = MassesTable[Passport[secondElement.Value].Mass].Element[Passport[secondElement.Value].Element] - Variables[firstElement.Value].Token;
                                                break;

                                            case OPSType.MAS:
                                                res = MassesTable[Passport[secondElement.Value].Mass].Element[Passport[secondElement.Value].Element]
                                                    - MassesTable[Passport[firstElement.Value].Mass].Element[Passport[firstElement.Value].Element];
                                                break;

                                            case OPSType.NUMBER:
                                                res = MassesTable[Passport[secondElement.Value].Mass].Element[Passport[secondElement.Value].Element] - firstElement.Value;
                                                break;

                                            default:
                                                break;
                                        }
                                        break;

                                    case OPSType.NUMBER:
                                        switch (firstElement.Type)
                                        {
                                            case OPSType.IDE:
                                                res = secondElement.Value - Variables[firstElement.Value].Token;
                                                break;

                                            case OPSType.NUMBER:
                                                res = secondElement.Value - firstElement.Value;
                                                break;

                                            case OPSType.MAS:
                                                res = MassesTable[Passport[firstElement.Value].Mass].Element[Passport[firstElement.Value].Element] - secondElement.Value;
                                                break;

                                            default:
                                                break;
                                        }
                                        break;
                                }

                                firstElement.Value = res;
                                firstElement.Type = OPSType.NUMBER;
                                stack.Push(firstElement);
                                z++;
                                break;


                            case '/':
                                firstElement = stack.Pop();     // Правое значение
                                secondElement = stack.Pop();    // Левое значение

                                switch (secondElement.Type)
                                {
                                    case OPSType.IDE:
                                        switch (firstElement.Type)
                                        {
                                            case OPSType.IDE:
                                                res = Variables[secondElement.Value].Token / Variables[firstElement.Value].Token;
                                                break;

                                            case OPSType.MAS:
                                                res = Variables[secondElement.Value].Token / MassesTable[Passport[firstElement.Value].Mass].Element[Passport[firstElement.Value].Element];
                                                break;

                                            case OPSType.NUMBER:
                                                res = Variables[secondElement.Value].Token / firstElement.Value;
                                                break;

                                            default:
                                                break;
                                        }
                                        break;

                                    case OPSType.MAS:
                                        switch (firstElement.Type)
                                        {
                                            case OPSType.IDE:
                                                res = MassesTable[Passport[secondElement.Value].Mass].Element[Passport[secondElement.Value].Element] / Variables[firstElement.Value].Token;
                                                break;

                                            case OPSType.MAS:
                                                res = MassesTable[Passport[secondElement.Value].Mass].Element[Passport[secondElement.Value].Element]
                                                    / MassesTable[Passport[firstElement.Value].Mass].Element[Passport[firstElement.Value].Element];
                                                break;

                                            case OPSType.NUMBER:
                                                res = MassesTable[Passport[secondElement.Value].Mass].Element[Passport[secondElement.Value].Element] / firstElement.Value;
                                                break;

                                            default:
                                                break;
                                        }
                                        break;

                                    case OPSType.NUMBER:
                                        switch (firstElement.Type)
                                        {
                                            case OPSType.IDE:
                                                res = secondElement.Value / Variables[firstElement.Value].Token;
                                                break;

                                            case OPSType.NUMBER:
                                                res = secondElement.Value / firstElement.Value;
                                                break;

                                            case OPSType.MAS:
                                                res = MassesTable[Passport[firstElement.Value].Mass].Element[Passport[firstElement.Value].Element] / secondElement.Value;
                                                break;

                                            default:
                                                break;
                                        }
                                        break;
                                }

                                firstElement.Value = res;
                                firstElement.Type = OPSType.NUMBER;
                                stack.Push(firstElement);
                                z++;
                                break;

                            default:
                                break;
                        }
                        break;
                    case OPSType.WR:
                        firstElement = stack.Pop();
                        switch (firstElement.Type)
                        {
                            case OPSType.IDE:
                                z--;
                                d = Lookup(new string(OPSMass[z].Element));
                                Console.WriteLine(Variables[d].Lexeme + "        = " + Variables[d].Token);
                                z = z + 2;
                                break;

                            case OPSType.NUMBER:
                                Console.WriteLine("Your number            = " + firstElement.Value);
                                z++;
                                break;

                            case OPSType.MAS:
                                string name = new string(MassesTable[Passport[firstElement.Value].Mass].Mass);
                                Console.WriteLine("[" + Passport[firstElement.Value].Element + "]" + " of " + name + " = " + MassesTable[Passport[firstElement.Value].Mass].Element[Passport[firstElement.Value].Element]);
                                z++;
                                break;

                            default:
                                break;
                        }
                        break;

                    case OPSType.RE:
                        firstElement = stack.Pop();
                        switch (firstElement.Type)
                        {
                            case OPSType.IDE:
                                z--;
                                d = Lookup(new string(OPSMass[z].Element));
                                Console.Write("Please, enter the value of var " + Variables[d].Lexeme + "      ");
                                Variables[d].Token = Convert.ToInt32(Console.ReadLine());
                                z += 2;
                                break;

                            case OPSType.MAS:
                                string name = new string(MassesTable[Passport[firstElement.Value].Mass].Mass);
                                Console.Write("Please, enter the " + "[" + Passport[firstElement.Value].Element + "]" + " value of array " + name);
                                MassesTable[Passport[firstElement.Value].Mass].Element[Passport[firstElement.Value].Element] = Convert.ToInt32(Console.ReadLine());
                                z++;
                                break;

                            default:
                                break;
                        }
                        break;

                    default:
                        break;
                }
            }
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            //Тесты
            ///Арифметика
            string test1 = "integer a, b, result; a =1; b = 5;result = b+3*(5/(a-b))+17/b;write(result); write(a); write(b);";
            string test2 = "integer a; integer b; read(b); read(a); b = a*(b+10); write(a); write(b);";
            //Массив
            string massTest1 = "integer index;  mass base[5]; read(index); read(base[index * index]); read(base[1]); write(base[index]); write(base[1]);";           
            //Ошибочные
            string wrongTest1 = "int a, b, result; a =1; b = 5;result = b+3*(5/(a-b))+17/b;write(result); write(a); write(b);";
            string wrongTest2 = "integer a, b, result; a =1; b = 5;result = ацацуа; write(a); write(b);";
            Intepreter Intepreter = new Intepreter();

            string programmtext = massTest1; // текст программы
            long programmSize;
            programmSize =programmtext.Length;

            Intepreter.ProgramText = new char[programmSize + 1];    //создание массива для текста программы
            char[] text = programmtext.ToCharArray();
            // переписываем в новый массив т.к. нужно ещё 1 ячейка памяти для @
            for (int i = 0; i < programmSize; i++)
            {
                Intepreter.ProgramText[i] = text[i];
            }

            Intepreter.ProgramText[Intepreter.ProgramText.Length - 1] = '@';    // конечный символ

            Intepreter.Initialize();    // заполнение таблицы ключевыми словами
            Intepreter.LookAhead = Intepreter.LexicalAnalyzer(); // 1 лексема
   //////////////////////// Cинтаксический анализ
            Intepreter.InitializeNames();

            Intepreter.OPSMass[Intepreter.OPSCounter].Element = new char[2];
            Intepreter.OPSMass[Intepreter.OPSCounter].Element = "@".ToCharArray();
            Intepreter.OPSMass[Intepreter.OPSCounter].Type = Intepreter.OPSType.SIGN;



            if (Intepreter.GlobError != true)
            {
///////////////////////////////////////// Вывод ОПС в консоль
                for (int f = 0; f < Intepreter.OPSCounter + 1; f++)
                {
                    for (int i = 0; i < Intepreter.OPSMass[f].Element.Length; i++)
                    {
                        if (Intepreter.OPSMass[f].Element[i] != '\0')
                            Console.Write(Intepreter.OPSMass[f].Element[i]);
                    }
                    Console.Write(" ");
                }

                Intepreter.OPSStack(); // Интерпритатор


                Console.WriteLine();
                Console.WriteLine("Program compiled successfully");
                Console.ReadLine();
            }

            else
            {
                Console.WriteLine();
                Console.WriteLine("Program compilation failed");
                Console.ReadLine();
            }
        }
    }
}