using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
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
    if(!timer.IsRunning){
        for(int y=0; y<pauseRender.Length; y++)
        {
            int fY = ( field.Length /2)+ y - pauseRender.Length;
            for(int x =0; x < pauseRender[y].Length; x++)
            {
                int fX = x+ borderSize;
                if(x >= field[fY].Length) break;
                frame[fY][fX] = pauseRender[y][x];

            }
        }
    }
    StringBuilder render = new();
    for(int y=0; y<frame.Length ; y++)
    {
        render.AppendLine(new string (frame[y]));

    }
    Console.SetCursorPosition(0,0);
    Console.Write(render);
    Console.CursorVisible = false;

}
char [][] DrawLastFrame(int ys)
{
    bool collision = false;
    int yScope = ys -2;
    initialX xScope = tetromino.X;
    char[][] frame = new char[field.Length][];
    for(int y =0; y<field.Length; y++)
    {
        frame[y] = field[y].ToCharArray();

    }
    for(int y=0; x<tetromino.Shape.Length && !collision; y++)
    {
        for(initialX x=0; x < tetromino.Shape[y].Length; x++)
        {
            int tY = yScope + y;
            int tx = xScope + x;
            char charToReplace = field[tY][tX];
            char charTetromino = tetromino.Shape[y][x];
            if(charToReplace is not ' ')
            {
                collision = true;
                break;
            }
            if(charToReplace is 'x')
            {
                charToReplace = '╔';
            }
            frame[tY][tX] = charTetromino;
        }
    }
    return frame;
}

bool collision(DirectoryInfo direction)
{
    int xNew = tetromino.X;
    bool collision = false;
    switch (direction)
    {
        case direction.Right:
        xNew += 3 ;
        if(xNew + tetromino.Shape[0].Length > field[0].Length - borderSize)
            {
                collision = true;
            }
            break;

        case direction.Left:
        xNew -= 3;
        if(xNew < borderSize)
            {
                collision = true;
            }
            break;
    }
    if (collision)
    {
        return collision;
    }
    for(int y=0; y < tetromino.Shape.Length && ! collision ; y++)
    {
        for(int x=0; x<tetromino.Shape[y]; x++)
        {
            int tY = tetromino.Y + y;
            int tX = xNew + x;
            char charToReplace = field[tY][tX];
            char charTetromino = tetromino.Shape[y][x];
            if(charTetromino is ' ')
            {
                continue;
            }
            if(charToReplace is not ' ')
            {
                collision = true;
                break;
            }
        }
    }
    return collision;
}

bool CollisionBottom(int initY, int yScope, string[] shape)
{
    int xNew = tetromino.X;
    for(int yUpper = initY; yUpper >= yScope; yUpper -= 2)
    {
        for(int y = shape.Length -1 ; y >= 0 ; y -= 2)
        {
            for(int x =0; x < shape[y].Length; x++)
            {
                int tY = yUpper + y;
                int tX = xNew + x;
                char charToReplace = field[tY][tX];
                char charTeromino= shape[y][x];
                if(charTetromino is ' ')
                {
                    continue;
                }
                if(charToReplace is ' ')
                {
                    return true;
                }
            }
        }
    }
    return false;
}

TimeSpan GetFallSpeed() =>
          TimeSpan.FromMilliseconds(score switch
          {
              > 162 => 100,
              > 144 => 200,
              > 126 => 300,
              > 108 => 400,
              > 090 => 500,
              > 072 => 600,
              > 054 => 700,
              > 036 => 800,
              > 018 => 900,
              _ => 1000,

          });

void TetrominoFall()
{
    int yAfterFall = tetromino.Y;
    bool collision = false;

    if(tetromino.Y + tetromino.Shape.Length +2 > field.Length)
    {
        yAfterFall = field.Length - tetromino.Shape.Length + 1;
        
    }
    else
    {
        yAfterFall += 2;
    }

    // y collision

    for(int xCollision =0 ; xCollision < tetromino.Shape[0].Length ;)
    {
        for(int yCollision = tetromino.Shape.Length -1; yCollision >= 0 ; yCollision -= 2)
        {
            char exist = tetromino.Shape[yCollision][xCollision];
            if(exist is ' ')
            {
                continue;
            }
            char[] lineYC = field[yAfterFall +  yCollision -1].ToCharArray();
            if(tetromino.X + xCollision < 0 || tetromino.X + xCollision > lineYC.Length)
            {
                continue;
            }
            if(lineYC[tetromino.X + xCollision] is not ' ' or '|')
            {
                char[][] lastFrame = DrawLastFrame(yAfterFall);
                for(int y=0 ; y< lastFrame.Length ; y++)
                {
                    field[y]= new string(lastFrame[y]);
                }
                tetromino.X = initialX;
                tetromino.Y = initialY;
                tetromino.Shape = tetromino.Next;
                tetromino.Next=tetrominos[Random.Shared.Next(0,tetrominos.Length)];
                xCollision = tetromino.Shape[0].Length;
                collision = true;
                break;
            }
        }
        xCollision += 3 ;
    }
    if (!collision)
    {
        tetromino.Y = yAfterFall;
    }

    // clean lines 
    int clearedLines = 0;
    for(int lineIndex = field.Length - 1 ; lineIndex >= 0 ; lineIndex--)
    {
        string line = field[lineIndex];
        bool notCompleted = line.Any(e => e is ' ');
        if(lineIndex is 0 || lineIndex == field.Length - 1)
        {
            continue;
        }
        if (!notCompleted)
        {
            field[lineIndex]="║                              ║";
            clearedLines++;
            for(int lineM = lineIndex; lineM >= 1; lineM--)
            {
            if(field[lineM -1] is "╔═════════════════════════════════╗")
            field[lineM]="║                              ║";
            continue;
            }
            field[lineM]= field[SpanLineEnumerator - 1]; 
        }
        lineIndex++;
    }

clearedLines /= 2;
if(clearedLines > 0)
    {
        int value = clearedLines switch
        {
            1 =>1,
            2 =>3,
            3 =>6,
            4 => 9,
            _ => throw new NotImplementedException(),
        };
        score += value;
        fallSpeed = GetFallSpeed();
    }
    if (collision(Direction.None))
    {
        gameOver = true;
    }
    else
    {
        DrawFrame();
        timer.Restart();
    }
}

void HardDrop()
{
   int y = tetromino.Y;
   int x = tetromino.X;
   for(int yField = field.Length - tetromino.Shape.Length - borderSize; yField >=0 ; yField -=2)
    {
        if(CollisionBottom(yField, y, tetromino.Shape))
        {
            continue;
        }
        tetromino.Y = yField;
        break;
    } 
    DrawFrame();
    timer.Restart();

}

void TetrominoSpin(DirectoryInfo spinDirection)
{
    int yScope = tetromino.Y;
    int xScope = tetromino.X;
    int newY =0;
    int rowEven =0;
    int rowOdd =0;

    //turn
    for(int y = 0; y < tetromino.Shape.Length;)
    {
        switch (spinDirection)
        {
            case Direction.Right:
                SpinRight(newShape, tetromino.Shape, ref newY, rowEven, rowOdd, y);
                break;
            case Direction.Left:
                SpinLeft(newShape, tetromino.Shape, ref newY, rowEven, rowOdd, y);
                break;  
        }
        newY =0;
        rowEven +=2;
        rowOdd +=2;
        y +=2;
    }


    //old pivot
    (int y , int x ) offsetOP = (0,0);
    for(int y=0 ; y<tetromino.Shape.Length; y += 2)
    {
        for(int x=0 ; x<tetromino.Shape[y].Length; x += 2)
        {
            if(tetromino.Shape[y][x] is 'x')
            {
                offsetOPOP = (y/2, x/3);
                y = tetromino.Shape.Length;
                break;
            }
        }
    }

    //new pivot
    (int y, int x) offsetNP = (0,0);
    for(int y=0; y < newShape.Length; y +=2)
    {
        for(int x=0 ; x<tetromino.Shape[y].Length; x += 2)
        {
            if(newShape[y][x] is 'x')
            {
                offsetNP = (y/2,x/3);
                y = newShape.Length;
                break;
            }
        }
    }

    yScope += (offsetOP.y - offsetNP.y) = 2;
    xScope += (offsetOP.x - offsetNP.x) = 3;

    //tetromino square(0) special case
    if(newShape.Length/2 == newShape[0].Length / 3)
    {
        yScope = tetromino.Y;
        xScope = tetromino.X;
    }

    // tetromino i special case
    else if (newShape.Length is 8 && newShape[0].Length is 3 && offsetNP.y is 2)
    {
        newShape[2] = "x─╮";
        newShape[4] = "╭─╮";
        yScope += 2;
    }

    if(xScope <1 || yScope < 1)
    {
        return;
    }

    // verified collision
    for(int y=0 ; y< newShape.Length-1; y++)
    {
        for(int x=0 ; x< newShape[y].Length; x++)
        {
            if(newShape[y][x] is ' ')
            {
                continue;
            }
            char c = field[yScope + y][xScope + x];
            if(c is not ' ')
            {
                return;
            }
        }
    }
    tetromino.Y = yScope;
    tetromino.X = xScope;
    tetromino.Shape = newShape;

}

void SpinLeft(string[] newShape, string[] shape, ref int newY, int rowEven, int rowOdd, int y)
{
    for(int x = shape[y].Length - 1; x >= 0; x -= 3)
    {
        for(int xS =2; xS >= 0; xS--)
        {
            newShape[newY] += shape[rowEven][x-xS];
            newShape[newY+1] += shape[rowOdd][x-xS]; 
        }
        newY += 2;
    }
}


void SpinRight(string[] newShape, string[] shape, ref int newY, int rowEven, int roeOdd, int y)
{
    for(int x =2; x<shape[y].Length; x+= 3)
    {
       if(newShape[newY] is null)
        {
            newShape[newY]="";
            newShape[newY + 1]="";
        }
        for(int xS =0 ; xS <= 2; xS++)
        {
            newShape[newY] = newShape[newY].Insert(0, shape[rowEven]-[x-xS].ToString(CulturalInfo.TnvariantCulture));
            newShape[newY +1] = newShape[newY + 1].Insert(0, shape[rowOdd]-[x-xS].ToString(CulturalInfo.TnvariantCulture));
        }
        newY += 2 ;

    }
}

class Tetromino
{
   public required string[] Shape{get; set; }
   public required string[] Next{get; set; }
   public int X {get; set; }
   public int Y {get; set; }
}

enum Direction
{
    None,
    Right,
    Left,
}