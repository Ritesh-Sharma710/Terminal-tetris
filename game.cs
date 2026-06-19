using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;

#region Constants

// Changed grid down to 32 rows to guarantee fit within a 35-row terminal height
string[] emptyField = new string[32];
emptyField[0] = "╔═════════════════════════════════╗"; 
for (int i = 1; i < 31; i++)
{
    emptyField[i] = "║                                 ║";
}
emptyField[^1] = "╚═════════════════════════════════╝"; 

string[] nextTetrominoBorder =
[
    "╔══════════╗",
    "║          ║",
    "║          ║",                 
    "║          ║",                 
    "║          ║",
    "║          ║",
    "║          ║",
    "║          ║",
    "║          ║",
    "╚══════════╝",
];

string[] scoreBorder =
[
    "╔══════════╗",                
    "║          ║",                 
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


const int boardSize = 1;

int initialX = (emptyField[0].Length / 2) - 3;
int initialY = 1;

// Tailored target sizing criteria exactly for a 50x35 grid context
int consoleWidthMin = 50; 
int consoleHeightMin = 35;

#endregion

Stopwatch timer = new();
bool closeRequested = false;
bool gameOver = false;
int score = 0;
TimeSpan fallSpeed;
string[] field = Array.Empty<string>();
Tetromino tetromino = new Tetromino { Shape = Array.Empty<string>(), Next = Array.Empty<string>() };
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

    bool mainMenuScreen = true;
    while (!closeRequested && mainMenuScreen)
    {
        Console.CursorVisible = false;
        if (Console.KeyAvailable)
        {
            switch (Console.ReadKey(true).Key)
            {
                case ConsoleKey.Enter: mainMenuScreen = false; break;
                case ConsoleKey.Escape: closeRequested = true; break;
            }
        }
    }

    if (closeRequested) break;

    Initialize();
    Console.Clear();
    DrawFrame();

    while (!closeRequested && !gameOver)
    {
        if (consoleWidth != Console.WindowWidth || consoleHeight != Console.WindowHeight)
        {
            consoleWidth = Console.WindowWidth;
            consoleHeight = Console.WindowHeight;
            if (!consoleTOOSmallScreen)
            {
                Console.Clear();
                DrawFrame();
            }
            else
            {
                consoleTOOSmallScreen = false;
            }
        }

        if (consoleWidth < consoleWidthMin || consoleHeight < consoleHeightMin)
        {
            if (!consoleTOOSmallScreen)
            {
                Console.Clear();
                Console.Write($"Please increase the size of console to at least {consoleWidthMin}x{consoleHeightMin}. Current size is {consoleWidth}x{consoleHeight}.");
                timer.Stop();
                consoleTOOSmallScreen = true;
            }
        }
        else if (consoleTOOSmallScreen)
        {
            consoleTOOSmallScreen = false;
            Console.Clear();
            DrawFrame();
        }

        HandlePlayerInput();

        if (closeRequested || gameOver)
        {
            break;
        }

        if (timer.IsRunning && timer.Elapsed > fallSpeed)
        {
            TetrominoFall();
            if (closeRequested || gameOver)
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

    Console.CursorVisible = false;
    bool gameOverScreen = true;
    while (!closeRequested && gameOverScreen)
    {
        switch (Console.ReadKey(true).Key)
        {
            case ConsoleKey.Enter: gameOverScreen = false; break;
            case ConsoleKey.Escape: closeRequested = true; break;
        }
    }
}

Console.Clear();
Console.WriteLine("Tetris was closed");
Console.CursorVisible = true;

#region Logic Methods

void Initialize()
{
    gameOver = false;
    score = 0;
    field = emptyField.ToArray();
    initialX = (field[0].Length / 2) - 3;
    initialY = 1;
    tetromino = new Tetromino()
    {
        Shape = tetrominos[Random.Shared.Next(0, tetrominos.Length)],
        Next = tetrominos[Random.Shared.Next(0, tetrominos.Length)],
        X = initialX,
        Y = initialY
    };
    fallSpeed = GetFallSpeed();
    timer.Restart();
}

void HandlePlayerInput()
{
    while (Console.KeyAvailable && !closeRequested)
    {
        switch (Console.ReadKey(true).Key)
        {
            case ConsoleKey.A or ConsoleKey.LeftArrow:
                if (timer.IsRunning && !collision(Direction.Left))
                {
                    tetromino.X -= 3;
                }
                DrawFrame();
                break;
            case ConsoleKey.D or ConsoleKey.RightArrow:
                if (timer.IsRunning && !collision(Direction.Right))
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
                    TetrominoSpin(Direction.Left);
                    DrawFrame();
                }
                break;
            case ConsoleKey.E:
                if (timer.IsRunning)
                {
                    TetrominoSpin(Direction.Right);
                    DrawFrame();
                }
                break;
            case ConsoleKey.P:
                if (timer.IsRunning)
                {
                    timer.Stop();
                    DrawFrame();
                }
                else if (!consoleTOOSmallScreen)
                {
                    timer.Start();
                    DrawFrame();
                }
                break;
            case ConsoleKey.Spacebar:
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
    bool isCollidingActive = false;
    char[][] frame = new char[field.Length][];

    for (int y = 0; y < field.Length; y++)
    {
        frame[y] = field[y].ToCharArray();
    }

    for (int y = 0; y < tetromino.Shape.Length && !isCollidingActive; y++)
    {
        for (int x = 0; x < tetromino.Shape[y].Length; x++)
        {
            int tY = tetromino.Y + y;
            int tX = tetromino.X + x;
            
            if (tY >= frame.Length || tX >= frame[tY].Length) continue;

            char charToReplace = field[tY][tX];
            char charTetromino = tetromino.Shape[y][x];
            if (charTetromino is ' ')
            {
                continue;
            }
            if (charToReplace is not ' ')
            {
                isCollidingActive = true;
                break;
            }
            if (charTetromino is 'x')
            {
                charTetromino = '╭';
            }
            frame[tY][tX] = charTetromino;
        }
    }

    // Draw Preview    
    for (int yField = field.Length - tetromino.Shape.Length - boardSize; yField >= 0; yField -= 2)
    {
        if (CollisionBottom(yField, tetromino.Y, tetromino.Shape))
        {
            continue;
        }
        for (int y = 0; y < tetromino.Shape.Length && !isCollidingActive; y++)
        {
            for (int x = 0; x < tetromino.Shape[y].Length; x++)
            {
                int tY = yField + y;
                if (tetromino.Y + tetromino.Shape.Length > tY)
                {
                    continue;
                }
                int tX = tetromino.X + x;
                char charToReplace = field[tY][tX];
                char charTetromino = tetromino.Shape[y][x];
                if (charTetromino is ' ')
                {
                    continue;
                }
                if (charToReplace is not ' ')
                {
                    isCollidingActive = true;
                    break;
                }
                frame[tY][tX] = '*';
            }
        }
        break;   
    }

    // Next Pane
    for (int y = 0; y < nextTetrominoBorder.Length; y++)
    {
        if (y < frame.Length)
        {
            frame[y] = [.. frame[y], .. nextTetrominoBorder[y]];
        }
    }
    for (int y = 0; y < tetromino.Next.Length; y++)
    {
        for (int x = 0; x < tetromino.Next[y].Length; x++)
        {
            int tY = y + boardSize + 1;
            int tX = field[0].Length + x + boardSize + 1;
            char charTetromino = tetromino.Next[y][x];
            if (charTetromino is 'x')
            {
                charTetromino = '╭';
            } 
            if (tY < frame.Length && tX < frame[tY].Length)
            {
                frame[tY][tX] = charTetromino;
            }
        }
    }

    // Score Pane
    for (int y = 0; y < scoreBorder.Length; y++)
    {
        int sY = nextTetrominoBorder.Length + y;
        if (sY < frame.Length)
        {
            frame[sY] = [.. frame[sY], .. scoreBorder[y]];
        }
    }
    char[] scoreRender = score.ToString(CultureInfo.InvariantCulture).ToCharArray();
    for (int scoreX = scoreRender.Length - 1; scoreX >= 0; scoreX--)
    {
        int sY = nextTetrominoBorder.Length + boardSize + 1;
        if (sY < frame.Length)
        {
            int sX = frame[sY].Length - (scoreRender.Length - scoreX) - boardSize - 1;
            if (sX >= 0 && sX < frame[sY].Length)
            {
                frame[sY][sX] = scoreRender[scoreX];
            }
        }
    }

    // Pause Graphic
    if (!timer.IsRunning)
    {
        for (int y = 0; y < pauseRender.Length; y++)
        {
            int fY = (field.Length / 2) + y - (pauseRender.Length / 2);
            if (fY >= 0 && fY < frame.Length)
            {
                for (int x = 0; x < pauseRender[y].Length; x++)
                {
                    int fX = x + boardSize + 2;
                    if (fX >= frame[fY].Length) break;
                    frame[fY][fX] = pauseRender[y][x];
                }
            }
        }
    }

    StringBuilder render = new();
    for (int y = 0; y < frame.Length; y++)
    {
        render.AppendLine(new string(frame[y]));
    }
    Console.SetCursorPosition(0, 0);
    Console.Write(render);
    Console.CursorVisible = false;
}

char[][] DrawLastFrame(int ys)
{
    bool isCollidingActive = false;
    int yScope = ys - 2;
    int xScope = tetromino.X;
    char[][] frame = new char[field.Length][];
    for (int y = 0; y < field.Length; y++)
    {
        frame[y] = field[y].ToCharArray();
    }
    for (int y = 0; y < tetromino.Shape.Length && !isCollidingActive; y++)
    {
        for (int x = 0; x < tetromino.Shape[y].Length; x++)
        {
            int tY = yScope + y;
            int tX = xScope + x;
            if (tY >= frame.Length || tX >= frame[tY].Length) continue;

            char charToReplace = field[tY][tX];
            char charTetromino = tetromino.Shape[y][x];
            if (charToReplace is not ' ')
            {
                isCollidingActive = true;
                break;
            }
            if (charTetromino is 'x')
            {
                charTetromino = '╭';
            }
            frame[tY][tX] = charTetromino;
        }
    }
    return frame;
}

bool collision(Direction direction)
{
    int xNew = tetromino.X;
    bool isCollidingActive = false;
    switch (direction)
    {
        case Direction.Right:
            xNew += 3;
            if (xNew + tetromino.Shape[0].Length > field[0].Length - boardSize)
            {
                isCollidingActive = true;
            }
            break;

        case Direction.Left:
            xNew -= 3;
            if (xNew < boardSize)
            {
                isCollidingActive = true;
            }
            break;
    }
    if (isCollidingActive)
    {
        return isCollidingActive;
    }
    for (int y = 0; y < tetromino.Shape.Length && !isCollidingActive; y++)
    {
        for (int x = 0; x < tetromino.Shape[y].Length; x++)
            {
            int tY = tetromino.Y + y;
            int tX = xNew + x;
            if (tY >= field.Length || tX >= field[0].Length) continue;

            char charToReplace = field[tY][tX];
            char charTetromino = tetromino.Shape[y][x];
            if (charTetromino is ' ')
            {
                continue;
            }
            if (charToReplace is not ' ')
            {
                isCollidingActive = true;
                break;
            }
        }
    }
    return isCollidingActive;
}

bool CollisionBottom(int initY, int yScope, string[] shape)
{
    int xNew = tetromino.X;
    for (int yUpper = initY; yUpper >= yScope; yUpper -= 2)
    {
        for (int y = shape.Length - 1; y >= 0; y -= 2)
        {
            for (int x = 0; x < shape[y].Length; x++)
            {
                int tY = yUpper + y;
                int tX = xNew + x;
                if (tY >= field.Length || tX >= field[0].Length) continue;

                char charToReplace = field[tY][tX];
                char charTetromino = shape[y][x];
                if (charTetromino is ' ')
                {
                    continue;
                }
                if (charToReplace is not ' ')
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
    bool collisionActive = false;

    if (tetromino.Y + tetromino.Shape.Length + 2 > field.Length)
    {
        yAfterFall = field.Length - tetromino.Shape.Length + 1;
    }
    else
    {
        yAfterFall += 2;
    }

    for (int xCollision = 0; xCollision < tetromino.Shape[0].Length;)
    {
        for (int yCollision = tetromino.Shape.Length - 1; yCollision >= 0; yCollision -= 2)
        {
            char exist = tetromino.Shape[yCollision][xCollision];
            if (exist is ' ')
            {
                continue;
            }
            int checkRow = yAfterFall + yCollision - 1;
            if (checkRow >= field.Length || checkRow < 0) continue;

            char[] lineYC = field[checkRow].ToCharArray();
            if (tetromino.X + xCollision < 0 || tetromino.X + xCollision >= lineYC.Length)
            {
                continue;
            }
            if (lineYC[tetromino.X + xCollision] is not ' ' and not '|')
            {
                char[][] lastFrame = DrawLastFrame(yAfterFall);
                for (int y = 0; y < lastFrame.Length; y++)
                {
                    field[y] = new string(lastFrame[y]);
                }
                tetromino.X = initialX;
                tetromino.Y = initialY;
                tetromino.Shape = tetromino.Next;
                tetromino.Next = tetrominos[Random.Shared.Next(0, tetrominos.Length)];
                xCollision = tetromino.Shape[0].Length;
                collisionActive = true;
                break;
            }
        }
        xCollision += 3;
    }
    if (!collisionActive)
    {
        tetromino.Y = yAfterFall;
    }

    // clear lines 
    int clearedLines = 0;
    for (int lineIndex = field.Length - 2; lineIndex >= 1; lineIndex--)
    {
        string line = field[lineIndex];
        bool notCompleted = line.Any(e => e is ' ');
        if (!notCompleted)
        {
            clearedLines++;
            for (int lineM = lineIndex; lineM >= 1; lineM--)
            {
                if (field[lineM - 1] == "╔═════════════════════════════════╗")
                {
                    field[lineM] = "║                                 ║";
                }
                else 
                {
                    field[lineM] = field[lineM - 1];
                }
            }
        }
    }

    clearedLines /= 2;
    if (clearedLines > 0)
    {
        int value = clearedLines switch
        {
            1 => 1,
            2 => 3,
            3 => 6,
            4 => 9,
            _ => 1
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
   for (int yField = field.Length - tetromino.Shape.Length - boardSize; yField >= 0; yField -= 2)
   {
        if (CollisionBottom(yField, y, tetromino.Shape))
        {
            continue;
        }
        tetromino.Y = yField;
        break;
   } 
   DrawFrame();
   timer.Restart();
}

void TetrominoSpin(Direction spinDirection)
{
    int yScope = tetromino.Y;
    int xScope = tetromino.X;
    int newY = 0;
    int rowEven = 0;
    int rowOdd = 0;
    
    // Safely allocate strings ahead of time to prevent null references during character insertions
    string[] newShape = new string[tetromino.Shape.Length];
    for (int i = 0; i < newShape.Length; i++)
    {
        newShape[i] = "";
    }

    // Process the rotation transformations safely
    for (int y = 0; y < tetromino.Shape.Length;)
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
        newY = 0;
        rowEven += 2;
        rowOdd += 2;
        y += 2;
    }

    // Track the old pivot target location
    (int y, int x) offsetOP = (0, 0);
    for (int yArr = 0; yArr < tetromino.Shape.Length; yArr += 2)
    {
        for (int xArr = 0; xArr < tetromino.Shape[yArr].Length; xArr += 3)
        {
            if (xArr < tetromino.Shape[yArr].Length && tetromino.Shape[yArr][xArr] is 'x')
            {
                offsetOP = (yArr / 2, xArr / 3);
                yArr = tetromino.Shape.Length;
                break;
            }
        }
    }

    // Track the new pivot target location 
    (int y, int x) offsetNP = (0, 0);
    for (int yArr = 0; yArr < newShape.Length; yArr += 2)
    {
        for (int xArr = 0; xArr < newShape[yArr].Length; xArr += 3)
        {
            if (xArr < newShape[yArr].Length && newShape[yArr][xArr] is 'x')
            {
                offsetNP = (yArr / 2, xArr / 3);
                yArr = newShape.Length;
                break;
            }
        }
    }

    // Apply the offset tracking safely
    yScope += (offsetOP.y - offsetNP.y) * 2;
    xScope += (offsetOP.x - offsetNP.x) * 3;

    if (newShape.Length / 2 == newShape[0].Length / 3)
    {
        yScope = tetromino.Y;
        xScope = tetromino.X;
    }
    else if (newShape.Length is 8 && newShape[0].Length is 3 && offsetNP.y is 2)
    {
        newShape[2] = "x─╮";
        newShape[4] = "╭─╮";
        yScope += 2;
    }

    // FIXED: Instead of crashing or returning completely empty arrays, we safely 
    // clamp bounds or cancel the spin action without corrupting the game state.
    if (xScope < boardSize) xScope = boardSize;
    if (xScope + newShape[0].Length > field[0].Length - boardSize) 
    {
        xScope = field[0].Length - boardSize - newShape[0].Length;
    }
    if (yScope < 1) yScope = 1;
    if (yScope + newShape.Length > field.Length - boardSize)
    {
        yScope = field.Length - boardSize - newShape.Length;
    }

    // Verify collision against surrounding items on the field grid
    for (int yArr = 0; yArr < newShape.Length; yArr++)
    {
        for (int xArr = 0; xArr < newShape[yArr].Length; xArr++)
        {
            if (newShape[yArr][xArr] is ' ') continue;
            
            int targetY = yScope + yArr;
            int targetX = xScope + xArr;

            if (targetY >= 0 && targetY < field.Length && targetX >= 0 && targetX < field[0].Length)
            {
                if (field[targetY][targetX] is not ' ')
                {
                    return; // Revert/Ignore rotation safely without breaking the block state
                }
            }
        }
    }

    // Persist valid transformation matrix state values safely
    tetromino.Y = yScope;
    tetromino.X = xScope;
    tetromino.Shape = newShape;
}

void SpinLeft(string[] newShape, string[] shape, ref int newY, int rowEven, int rowOdd, int y)
{
    for (int x = shape[y].Length - 3; x >= 0; x -= 3)
    {
        if (newY + 1 < newShape.Length)
        {
            newShape[newY] += shape[rowEven].Substring(x, 3);
            newShape[newY + 1] += shape[rowOdd].Substring(x, 3); 
        }
        newY += 2;
    }
}

void SpinRight(string[] newShape, string[] shape, ref int newY, int rowEven, int rowOdd, int y)
{
    for (int x = 0; x < shape[y].Length; x += 3)
    {
        if (newY + 1 < newShape.Length)
        {
            newShape[newY] = shape[rowEven].Substring(x, 3) + newShape[newY];
            newShape[newY + 1] = shape[rowOdd].Substring(x, 3) + newShape[newY + 1];
        }
        newY += 2;
    }
}

#endregion

#region Component Types

class Tetromino
{
   public required string[] Shape { get; set; }
   public required string[] Next { get; set; }
   public int X { get; set; }
   public int Y { get; set; }
}

enum Direction
{
    None,
    Right,
    Left,
}

#endregion