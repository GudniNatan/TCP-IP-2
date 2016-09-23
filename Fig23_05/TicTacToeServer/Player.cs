﻿using System;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO;

namespace TicTacToeServer
{
    // class Player represents a tic-tac-toe player
    public class Player
    {
        internal Socket connection; // Socket for accepting a connection    
        private NetworkStream socketStream; // network data stream          
        private TicTacToeServerForm server; // reference to server          
        private BinaryWriter writer; // facilitates writing to the stream   
        private BinaryReader reader; // facilitates reading from the stream 
        private int number; // player number                                
        private char mark; // player’s mark on the board                    
        internal bool threadSuspended = true; // if waiting for other player

        // constructor requiring Socket, TicTacToeServerForm and int 
        // objects as arguments
        public Player(Socket socket, TicTacToeServerForm serverValue,
           int newNumber)
        {
            mark = (newNumber == 0 ? 'X' : 'O');
            connection = socket;
            server = serverValue;
            number = newNumber;

            // create NetworkStream object for Socket      
            socketStream = new NetworkStream(connection);

            // create Streams for reading/writing bytes
            writer = new BinaryWriter(socketStream);
            reader = new BinaryReader(socketStream);
        } // end constructor

        // signal other player of move
        public void OtherPlayerMoved(int location)
        {
            // signal that opponent moved                     
            writer.Write("Opponent moved.");
            writer.Write(location); // send location of move
            if (!string.IsNullOrEmpty(server.GameOver()))
            {
                writer.Write(server.GameOver());
            }
        } // end method OtherPlayerMoved

        // allows the players to make moves and receive moves
        // from the other player
        public void Run()
        {
            bool done = false;

            // display on the server that a connection was made
            server.DisplayMessage("Player " + (number == 0 ? 'X' : 'O')
               + " connected\r\n");

            // send the current player's mark to the client
            writer.Write(mark);

            // if number equals 0 then this player is X,                
            // otherwise O must wait for X's first move                 
            writer.Write("Player " + (number == 0 ?
               "X connected.\r\n" : "O connected, please wait.\r\n"));

            // X must wait for another player to arrive
            if (mark == 'X')
            {
                writer.Write("Waiting for another player.");

                // wait for notification from server that another 
                // player has connected
                lock (this)
                {
                    while (threadSuspended)
                        Monitor.Wait(this);
                } // end lock               

                writer.Write("Other player connected. Your move.");
            } // end if

            // play game
            while (!done)
            {
                // wait for data to become available
                while (connection.Available == 0)
                {
                    Thread.Sleep(100);
                    if (server.disconnected)
                        return;
                } // end while

                // receive data
                string message = reader.ReadString();
                int location = -1;
                if (message.Split().Length == 2 && message.Split()[0] == "loc" && int.TryParse(message.Split()[1], out location))
                {
                    location = Convert.ToInt32(message.Split()[1]);                    
                }
                else if (message == "RESTART")
                {
                    server.Restart();
                }
                else if (message == "EXIT")
                {
                    done = true;
                }
                // if the move is valid, display the move on the
                // server and signal that the move is valid
                if (location != -1 && server.ValidMove(location, number))
                {
                    server.DisplayMessage("loc: " + location + "\r\n");
                    writer.Write("Valid move.");
                } // end if
                else if (location != -1) // signal that the move is invalid
                    writer.Write("Invalid move, try again.");

                // if game is over, set done to true to exit while loop

                if (server.GameOver() != null)
                {
                    writer.Write(server.GameOver());
                }
            } // end while loop

            // close the socket connection
            writer.Close();
            reader.Close();
            socketStream.Close();
            connection.Close();
        } // end method Run
        public void Message(string message)
        {
            writer.Write(message);
        }
    } // end class Player
}
