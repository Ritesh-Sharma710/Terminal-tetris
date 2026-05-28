using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
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
        
    }

}