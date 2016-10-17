using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Lidgren.Network;
using System.Collections.Generic;
using System;
using Newtonsoft.Json;
using GameServerConsole;
using NSLoader;
using Utilities;

namespace LingrenGame
{
    public class Game1 : Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;

        private NetPeerConfiguration ClientConfig;
        private NetClient client;
        private string InGameMessage = string.Empty;
        private SpriteFont font;
        GamePlayer thisPlayer;
        List<GamePlayer> OtherPlayers = new List<GamePlayer>();

        FadeTextManager fadeTextManager;

        Dictionary<string, Texture2D> playerTextures = new Dictionary<string, Texture2D>();

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
        }

        protected override void Initialize()
        {
            // TODO: Add your initialization logic here

            ClientConfig = new NetPeerConfiguration("s00146980");
            //for the client
            ClientConfig.EnableMessageType(NetIncomingMessageType.DiscoveryResponse);
            client = new NetClient(ClientConfig);
            client.Start();
            InGameMessage = "This Client has a unique id of " + client.UniqueIdentifier.ToString();
            // Note Named parameters for more readable code
            //client.Connect(host: "127.0.0.1", port: 12345);
            //search in local network at port 50001
            client.DiscoverLocalPeers(12346);

            base.Initialize();
        }

        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);
            font = Content.Load<SpriteFont>("GameFont");
            this.Services.AddService(font);
            this.Services.AddService(spriteBatch);

            fadeTextManager = new FadeTextManager(this);
            //new FadeText(this, Vector.Zero, Bitch Hello!!);

            playerTextures = Loader.ContentLoad<Texture2D>(Content, @".\PlayerImages\");
        }

        protected override void UnloadContent()
        {
          
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
            {
                
                NetOutgoingMessage sendMsg = client.CreateMessage();
                PlayerData playerLeaving = thisPlayer.PlayerDataPacket;
                playerLeaving.header = "leaving";
                string json = JsonConvert.SerializeObject(playerLeaving);

                Exit();

            }
            if (Keyboard.GetState().IsKeyDown(Keys.Enter))
            {
                InGameMessage = "Sending Message";
                NetOutgoingMessage sendMsg = client.CreateMessage();
                sendMsg.Write("Hello there from client at " + gameTime.TotalGameTime.ToString());
                client.SendMessage(sendMsg, NetDeliveryMethod.ReliableOrdered);
            }

            foreach (var p in OtherPlayers)
                p.ChangePosition(p.position);

            if (Keyboard.GetState().IsKeyDown(Keys.A))
                thisPlayer.position.X = thisPlayer.Position.X - 1;
            if (Keyboard.GetState().IsKeyDown(Keys.D))
                thisPlayer.position.X = thisPlayer.Position.X + 1;
            if (Keyboard.GetState().IsKeyDown(Keys.S))
                thisPlayer.position.Y = thisPlayer.Position.Y + 1;
            if (Keyboard.GetState().IsKeyDown(Keys.W))
                thisPlayer.position.Y = thisPlayer.Position.Y - 1;
            CheckMessages();

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);
            spriteBatch.Begin();
            spriteBatch.DrawString(font, InGameMessage, new Vector2(10, 10), Color.White);

            if (thisPlayer != null)
            {
                spriteBatch.Draw(playerTextures[thisPlayer.ImageName], thisPlayer.Position, Color.White);
                spriteBatch.DrawString(font,playerTextures[thisPlayer.gamertag].ToString(),new Vector2(thisPlayer.Position.X, thisPlayer.Position.Y - 20), Color.Black);
            }

            foreach (GamePlayer other in OtherPlayers)
            {
                spriteBatch.Draw(playerTextures[other.ImageName], other.Position, Color.White);
                spriteBatch.DrawString(font, playerTextures[other.gamertag].ToString(), new Vector2(other.Position.X, other.Position.Y - 20), Color.Black);
            }
            spriteBatch.End();

            base.Draw(gameTime);
        }
        #region Lidgren message handlers
        private void CheckMessages()
        {
            NetIncomingMessage ServerMessage;
            if ((ServerMessage = client.ReadMessage()) != null)
            {
                switch (ServerMessage.MessageType)
                {
                    case NetIncomingMessageType.Data:
                        // handle custom messages
                        string message = ServerMessage.ReadString();
                        //InGameMessage = "Data In " + message;
                        process(message);
                        break;
                    case NetIncomingMessageType.DiscoveryResponse:
                        InGameMessage = ServerMessage.ReadString();
                        if (InGameMessage == ClientConfig.AppIdentifier)
                        {
                            client.Connect(ServerMessage.SenderEndPoint);
                            InGameMessage = "Connected to " + ServerMessage.SenderEndPoint.Address.ToString();
                            if (thisPlayer == null)
                            {
                                string ImageName = "Badges_" + Utility.NextRandom(0, playerTextures.Count - 1);
                                thisPlayer = new GamePlayer(client, Guid.NewGuid(), ImageName, "Rebecca",
                                              new Vector2(Utility.NextRandom(100, GraphicsDevice.Viewport.Width - 100),
                                                           Utility.NextRandom(100, GraphicsDevice.Viewport.Height - 100)));

                            }
                        }

                        break;
                    case NetIncomingMessageType.StatusChanged:
                        // handle connection status messages
                        switch (ServerMessage.SenderConnection.Status)
                        {
                            /* .. */
                        }
                        break;

                    case NetIncomingMessageType.DebugMessage:
                        // handle debug messages
                        // (only received when compiled in DEBUG mode)
                        //InGameMessage = ServerMessage.ReadString();
                        break;

                        /* .. */

                        InGameMessage = "unhandled message with type: "
                            + ServerMessage.MessageType.ToString();
                        break;
                }
            }
        }
        private void process(string v)
        {
            // Need a try catch here
            PlayerData otherPlayer = JsonConvert.DeserializeObject<PlayerData>(v);
            // if it's the same player back just ignore it
            if (otherPlayer.playerID == thisPlayer.PlayerID)
                return;

            switch (otherPlayer.header)
            {
                case "Joined":
                    // Add the player to this game as another player
                    string ImageName = "Badges_" + Utility.NextRandom(0, playerTextures.Count - 1);
                    GamePlayer newPlayer = new GamePlayer(client, otherPlayer.gamerTag, otherPlayer.imageName, otherPlayer.playerID, new Vector2(otherPlayer.X, otherPlayer.Y));
                    OtherPlayers.Add(newPlayer);

                    break;
                default:
                    break;
            }

        }
        #endregion

    }
}
