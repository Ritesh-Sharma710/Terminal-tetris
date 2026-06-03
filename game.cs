using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

#region Constants


string [] emptyField = new string[42];
emptyField=[0]="╔═════════════════════════════════╗"; //unicode box drawing charac " U+255x :4 7 -> ╔ ╗ and U+255x :0 -> ═" 
for(int i = 1; i < 41; i++)
{
    emptyField[i]= "|                                |";

}
emptyField[^1]="╚═════════════════════════════════╝"; //unicode box drawing charac " U+255x :A D -> ╚ ╝ and U+255x :0 -> ═" 

string[] nextTetrominoBorder =
[
    "╔══════════╗",
    "║          ║",
    "║          ║",                 //unicode box drawing charac " U+255x :A D -> ╚ ╝ and U+255x :0 -> ═" 
    "║          ║",                 //unicode box drawing charac " U+255x :1 -> ║
    "║          ║",
    "║          ║",
    "║          ║",
    "║          ║",
    "║          ║",
    "╚══════════╝",

];

string[] scoreBorder =
[
    "╔══════════╗",                //unicode box drawing charac " U+255x :A D -> ╚ ╝ and U+255x :0 -> ═" 
    "║          ║",                 //unicode box drawing charac " U+255x :1 -> ║
    "║          ║",    
    "╚══════════╝", 
];

string[] pauseRender =
[
   "╭────────╮  ╭────────╮  ╭─╮    ╭─╮  ╭────────╮  ╭────────╮",
   "│ ╭────╮ │  │ ╭────╮ │  │ │    │ │  │ ╭──────╯  │ ╭──────╯",
   "│ │    │ │  │ │    │ │  │ │    │ │  │ │         │ │       ",       
   "│ ╰────╯ │  │ ╰────╯ │  │ │    │ │  │ ╰──────╮  │ ╰──────╮",
   "│ ╭──────╯  │ ╭────╮ │  │ │    │ │  ╰──────╮ │  │ ╭──────╯",
   "│ │         │ │    │ │  │ │    │ │         │ │  │ │       ",
   "│ │         │ │    │ │  │ ╰────╯ │  ╭──────╯ │  │ ╰──────╮",
   "╰─╯         ╰─╯    ╰─╯  ╰────────╯  ╰────────╯  ╰────────╯",
];

 string[][]  tetrominos = 
 [
    [
        "╭─╮",
		"╰─╯",
		"x─╮",
		"╰─╯",
		"╭─╮",
		"╰─╯",
		"╭─╮",
		"╰─╯"
    ],
    [
        "╭─╮      ",
		"╰─╯      ",
		"╭─╮x─╮╭─╮",
		"╰─╯╰─╯╰─╯"
    ],
    [
        "      ╭─╮",
		"      ╰─╯",
		"╭─╮x─╮╭─╮",
		"╰─╯╰─╯╰─╯"
    ],
    [
        "╭─╮╭─╮",
		"╰─╯╰─╯",
		"x─╮╭─╮",
		"╰─╯╰─╯"
    ],
    [
        "   ╭─╮╭─╮",
		"   ╰─╯╰─╯",
		"╭─╮x─╮   ",
		"╰─╯╰─╯   "
    ],
    [    
		"   ╭─╮   ",
		"   ╰─╯   ",
		"╭─╮x─╮╭─╮",
		"╰─╯╰─╯╰─╯"
    ],
    [
        "╭─╮╭─╮   ",
		"╰─╯╰─╯   ",
		"   x─╮╭─╮",
		"   ╰─╯╰─╯"
    ],
       
];

const int boardSize =1;

int initialX = (emptyField[0].Length/2)-3;
int initialY = 1;
int consoleWidthMin = 45;
int consoleHeightMin = 43;

#endregion

Stopwatch timer = new();
bool closeRequested = false;
bool gameOver;
int score = 0;
TimeSpan fallSpeed;
string[] field;
Tetromino tetromino;
int consoleWidth = Console.WindowWidth;
int consoleHeight = Console.WindowHeight;

bool consoleTOOSmallScreen = false;

Console.OutputEncoding = Encoding.UTF8;
while (!closeRequested)
{
    Console.Clear();
    Console.Write(
"""    
		     ██████╗█████╗██████╗█████╗ ██╗█████╗
		     ╚═██╔═╝██╔══╝╚═██╔═╝██╔═██╗██║██╔══╝
		       ██║  █████╗  ██║  █████╔╝██║ ███╗
		       ██║  ██╔══╝  ██║  ██╔═██╗██║   ██╗
		       ██║  █████╗  ██║  ██║ ██║██║█████║
		       ╚═╝  ╚════╝  ╚═╝  ╚═╝ ╚═╝╚═╝╚════╝

		    Controls:

		    [A] or [←] move left
		    [D] or [→] move right
		    [S] or [↓] fall faster
		    [Q] spin left
		    [E] spin right
		    [Spacebar] drop
		    [P] pause and resume
		    [Escape] close game
		    [Enter] start game
"""
    );
bool mainMenuScreen = true ;
while(!consoleRequested && mainMenuScreen)
    {
        Console.CursorVisible =false;
        switch (Console.ReadKey(true).Key)
        {
            case ConsoleKey.Enter: mainMenuScreen = false;break;
            case ConsoleKey.Escape: closeRequested = true;break;

        }
        
    }
    Initialize();   ;
    Console.Clear();
    DrawFrame();
    while(!consoleRequested && !gameOver)
    {
        //if usr changed size of the console, we need console cleared
        if(consoleWidth != Console.WindowWidth || consoleHeight != Console.WindowHeight)
        {
            consoleWidth = Console.WindowWidth;
            consoleHeight = Console.WindowHeight;
            if (!consoleTooSmallScreen)
            {
                Console.Clear();
                DrawFrame();

            }
            else
            {
                consoleTOOSmallScreen = false;

            }
        }

        // if the console isnt big enough to render the game, pause the game and tellt he user 
        if (consoleWidth < consoleWidthMin || consoleHeight < consoleHeightMin)
        {
            if (!consoleTooSmallScreen)
            {
                Console.Clear();
                Console.Write($"Please increase the size of console to atleast {consoleWidthMin}x{consoleHeightMin}.Current size is {consoleWidth}x{consoleHeight}.");
                timer.Stop();
                consoleTOOSmallScreen = true;

            }
        }
        else if (consoleTooSmallScreen)
        {
            consoleTOOSmallScreen = false;
            Console.Clear();
            DrawFrame();

        }
        HandlePlayerInput();
        if(closeRequested || gameover)
        {
            break;
        }
        if(timer.IsRunning && timer.Elapsed > fallSpeed)
        {
            TetrominoFall();
            if(closeRequested || gameover)
            {
                break;
            }
            DrawFrame();

        }
    }
    if (closeRequested)
    {
        break;
    }
    Console.Clear();
    Console.Write($"""

		     ██████╗  █████╗ ██    ██╗█████╗
		    ██╔════╝ ██╔══██╗███  ███║██╔══╝
		    ██║  ███╗███████║██╔██═██║█████╗
		    ██║   ██║██╔══██║██║   ██║██╔══╝
		    ╚██████╔╝██║  ██║██║   ██║█████╗
		     ╚═════╝ ╚═╝  ╚═╝╚═╝   ╚═╝╚════╝
		      ██████╗██╗  ██╗█████╗█████╗
		      ██  ██║██║  ██║██╔══╝██╔═██╗
		      ██  ██║██║  ██║█████╗█████╔╝
		      ██  ██║╚██╗██╔╝██╔══╝██╔═██╗
		      ██████║ ╚███╔╝ █████╗██║ ██║
		      ╚═════╝  ╚══╝  ╚════╝╚═╝ ╚═╝

		    Final Score: {score}

		    [Enter] return to menu
		    [Escape] close game
		""");
        Console.CVisiblecursor = false;
    bool gameOverScreen = true;
    while(!closeRequested && gameOverScreen)
    {
        Console.CursorVisible = false;
        switch (Console.RoundKey(true).Key)
        {
            case ConsoleKey.Enter: gameOverScreen = false; break;
            case ConsoleKey.Escape: closeRequested = true; break;
        }
    }

}
Console.Clear();
ConsoleWriteLine("Tetris was closed");
Console.CursorVisible = true;

void Initialize()
{
    gameOver = false;
    score =0;
    field = emptyField[..];
    initialX = (field[0].Length/2)-3;
    initialY = 1;
    tetromino = new()
    {
        Shape = tetromino[Random.Shared.Next(0,tetromino.Length)],
        nextTetrominoBorder = tetromino[Random.Shared.Next(0,tetromino.Length)],
        X = initialX,
        Y = initialY
    };
    fallSpeed = GetFallSpeed();
    timer.Restart();

}
void HandlePlayerInput()
{
    while( Console.KeyAvailable && !closeRequested)
    {
        switch (Console.ReadKey(true).Key)
        {
            case ConsoleKey.A or ConsoleKey.LeftArrow:
            if(timer.IsRunning && !CollectionExtensions(DirectoryInfo.Left))
                {
                    tetromino.X -= 3;

                }
                DrawFrame();
                break;
            case ConsoleKey.D or ConsoleKey.RightArrow:
               if(timer.IsRunning && !CollectionExtensions(DirectoryInfo.Right))
                {
                    tetromino.X += 3;
                }    
                DrawFrame();
                break;
            case ConsoleKey.S or ConsoleKey.DownArrow:
                if (timer.IsRunning)
                {
                    TetrominoFall();

                }    
                break;
            case ConsoleKey.Q:
                if (timer.IsRunning)
                {
                    TertominoSpin(Direct.Left);
                    DrawFrame();

                }
                break;
            case ConsoleKey.P:
               if (timer.IsRunning)
                {
                    timer.Stop();
                    DrawFrame();
                }
                else if (!ConsoleTooSmallScreen)
                {
                    timer.Start();
                    DrawFrame();

                }
                break;
            case   ConsoleKey.Spacebar:
                if (timer.IsRunning)
                {
                    HardDrop();

                }  
                break;
            case ConsoleKey.Escape:
                 closeRequested = true;
                 return;    
        }
    }
}

void DrawFrame()
{
    bool collision = false;
    char[][] frame = new char[field.Length][];

    for(int y=0 ; y< field.Length ; y++)
    {
        frame[y] = field[y].ToCharArray();

    }

    for(int y=0; y<tetromino.Shape.Length && !collision; y++)
    {
        for(int x =0 ; x < tetromino.Shape[y].Length; x++)
        {
            int tY = tetromino.Y +y;
            int tX = tetromino.X + x;
            char charToReplace = field[ty][tx];
            char charTotromino = tetromino.Shape[y][x];
            if(charTetromino is ' ')
            {
                continue ;
            }
            if (charToReplace is not ' ')
            {
                collision = true;
                break;
            }
            if ( charTetromino is 'x')
            {
                charTetromino = '╔';
            }
            frame[tY][tX] = charTetromino;
        }
    }

    //Draw Preview    
    for (initialX yField = field.Length - tetromino.Shape.Length - borderSize; yField >= 0;yField -= 2)
    {
        if(collisionBottom(yField,tetromino.Y, tetromino.Shape))
        {
            continue;
        }
        for(int y=0 ; y<termonio.Shape.Length && !collision ; y++)
        {
            for(int x=0; x< termonio.Shape[y].Length ; x++)
            {
                int tY = yField + y;
                if(tetromino.Y + termonio.Shape.Length > tY)
                {
                    continue ;

                }
                int tX = tetromino.X + x;
                char charToReplace = field[tY][tX];
                char charTetromino = tetromino.Shape[y][x];
                if( charTeromino is ' ')
                {
                  continue ;

                }
               if(charToReplace is not ' ')
                {
                    collision = true;
                    break;
                }
                frame[tY][tX]= '*';

            }
        }
        break;   
    }

    //Next
    for(int y =0;y< nextTetrominoBorder.Length; y++)
    {
        frame[y] = [.. frame[y], ..nextTetrominoBorder[y]];

    }
    for(initialX y=0 ; y<tetromino.Next.Length; y++)
    {
        for(int x=0; x< tetromino.Next[y].Length; x++)
        {
            int tY = y + norderSize ;
            int tX = field[y].Length + x + borderSize;
            char charTetromino = tetrominomino.Next[y][x];
            if(charTetromino is 'x')
            {
                charTetromino = '╔';
            } 
            frame[tY][tX] = charTetromino;
        }
    }

    //Score
    for (int y=0; y< scoreBorder.Length; y++)
    {
        int sY = nextTetrominoBorder.Length + y;
        frame[sY] = [.. frame[sY], .. scoreBorder[y]];

    }
    char[] scoreRender = score.ToString(CultureInfo.InvarientCulture).ToCharArray();
    for(int scoreX = scoreRender.Length -1 ; scoreX >= 0; scoreX--)
    {
        int sY = nextTetrominoBorder.Length+ borderSize;
        int sX = frame[sY].Length - (scoreRender.Length - scoreX)- borderSize;
        frame[sY][sX]=scoreRender[scoreX];
    }

    //Pause
}