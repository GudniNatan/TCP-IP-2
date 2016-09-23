// Fig. 23.5: TicTacToeServer.cs
// This class maintains a game of Tic-Tac-Toe for two
// client applications.
using System;
using System.Windows.Forms;
using System.Net;        
using System.Net.Sockets;
using System.Threading;  
using System.IO;

namespace TicTacToeServer
{

    public partial class TicTacToeServerForm : Form
    {
        public TicTacToeServerForm()
        {
            InitializeComponent();
        } // end constructor

        private byte[] board; // the local representation of the game board  
        private Player[] players; // two Player objects                      
        private Thread[] playerThreads; // Threads for client interaction    
        private TcpListener listener; // listen for client connection        
        private int currentPlayer; // keep track of whose turn it is         
        private Thread getPlayers; // Thread for acquiring client connections
        internal bool disconnected = false; // true if the server closes     

        // initialize variables and thread for receiving clients
        private void TicTacToeServerForm_Load(object sender, EventArgs e)
        {
            board = new byte[9];
            players = new Player[2];
            playerThreads = new Thread[2];
            currentPlayer = 0;

            // accept connections on a different thread         
            getPlayers = new Thread(new ThreadStart(SetUp));
            getPlayers.Start();
        } // end method TicTacToeServerForm_Load

        // notify Players to stop Running
        private void TicTacToeServerForm_FormClosing(object sender,
           FormClosingEventArgs e)
        {
            disconnected = true;
            System.Environment.Exit(System.Environment.ExitCode);
        } // end method TicTacToeServerForm_FormClosing

        // delegate that allows method DisplayMessage to be called
        // in the thread that creates and maintains the GUI       
        private delegate void DisplayDelegate(string message);

        // method DisplayMessage sets displayTextBox's Text property
        // in a thread-safe manner
        internal void DisplayMessage(string message)
        {
            // if modifying displayTextBox is not thread safe
            if (displayTextBox.InvokeRequired)
            {
                // use inherited method Invoke to execute DisplayMessage
                // via a delegate                                       
                Invoke(new DisplayDelegate(DisplayMessage),
                   new object[] { message });
            } // end if
            else // OK to modify displayTextBox in current thread
                displayTextBox.Text += message;
        } // end method DisplayMessage

        // accepts connections from 2 players
        public void SetUp()
        {
            DisplayMessage("Waiting for players...\r\n");

            // set up Socket                                           
            listener =
               new TcpListener(IPAddress.Parse("127.0.0.1"), 50000);
            listener.Start();

            // accept first player and start a player thread              
            players[0] = new Player(listener.AcceptSocket(), this, 0);
            playerThreads[0] =
               new Thread(new ThreadStart(players[0].Run));
            playerThreads[0].Start();

            // accept second player and start another player thread       
            players[1] = new Player(listener.AcceptSocket(), this, 1);
            playerThreads[1] =
               new Thread(new ThreadStart(players[1].Run));
            playerThreads[1].Start();

            // let the first player know that the other player has connected
            lock (players[0])
            {
                players[0].threadSuspended = false;
                Monitor.Pulse(players[0]);
            } // end lock                                                   
        } // end method SetUp

        // determine if a move is valid
        public bool ValidMove(int location, int player)
        {
            // prevent another thread from making a move
            lock (this)
            {
                // while it is not the current player's turn, wait
                while (player != currentPlayer)
                    Monitor.Wait(this);

                // if the desired square is not occupied
                if (!IsOccupied(location))
                {
                    // set the board to contain the current player's mark
                    board[location] = (byte)(currentPlayer == 0 ?
                       'X' : 'O');

                    // set the currentPlayer to be the other player
                    currentPlayer = (currentPlayer + 1) % 2;

                    // notify the other player of the move                
                    players[currentPlayer].OtherPlayerMoved(location);

                    // alert the other player that it's time to move
                    Monitor.Pulse(this);
                    return true;
                } // end if
                else
                    return false;
            } // end lock
        } // end method ValidMove

        // determines whether the specified square is occupied
        public bool IsOccupied(int location)
        {
            if (board[location] == 'X' || board[location] == 'O')
                return true;
            else
                return false;
        } // end method IsOccupied

        // determines if the game is over
        public string GameOver()
        {
            int sum = 0;
            foreach (var item in board)
	        {
		        sum += item;
	        }
            // place code here to test for a winner of the game
            if (board[0] + board[1] + board[2] == 264 || board[3] + board[4] + board[5] == 264 || board[6] + board[7] + board[8] == 264 ||
                board[0] + board[3] + board[6] == 264 || board[1] + board[4] + board[7] == 264 || board[2] + board[5] + board[8] == 264 ||
                board[0] + board[4] + board[8] == 264 || board[2] + board[4] + board[6] == 264)
            {
                DisplayMessage("\nX Vinnur");

                return "X Vinnur";
            }
            else if (board[0] + board[1] + board[2] == 237 || board[3] + board[4] + board[5] == 237 || board[6] + board[7] + board[8] == 237 ||
                board[0] + board[3] + board[6] == 237 || board[1] + board[4] + board[7] == 237 || board[2] + board[5] + board[8] == 237 ||
                board[0] + board[4] + board[8] == 237 || board[2] + board[4] + board[6] == 237)
	        {
                DisplayMessage("\nO Vinnur");
                return "O Vinnur";
	        }
            else if (sum == 756)
            {
                DisplayMessage("\nJafntefli");
                return "Jafntefli";
            }
            else
                return null;
        } // end method GameOver
        public void Restart()
        {
            board = new byte[9];
            currentPlayer = 0;
            foreach (var player in players)
            {
                player.Message("RESTART");
            }
        }
    } // end class TicTacToeServerForm
}

/**************************************************************************
 * (C) Copyright 1992-2006 by Deitel & Associates, Inc. and               *
 * Pearson Education, Inc. All Rights Reserved.                           *
 *                                                                        *
 * DISCLAIMER: The authors and publisher of this book have used their     *
 * best efforts in preparing the book. These efforts include the          *
 * development, research, and testing of the theories and programs        *
 * to determine their effectiveness. The authors and publisher make       *
 * no warranty of any kind, expressed or implied, with regard to these    *
 * programs or to the documentation contained in these books. The authors *
 * and publisher shall not be liable in any event for incidental or       *
 * consequential damages in connection with, or arising out of, the       *
 * furnishing, performance, or use of these programs.                     *
 *************************************************************************/