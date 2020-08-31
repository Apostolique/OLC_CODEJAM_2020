﻿using GameCore.Entities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PandaMonogame;
using PandaMonogame.Assets;
using System;
using System.Collections.Generic;
using System.Text;

namespace GameCore
{
    public class WorldManager
    {
        public static int StartingMiners = 2;
        public static int WorldWidth = 50000;
        public static int WorldHeight = 50000;
        public static int AsteroidRegionWidth = 250;
        public static int AsteroidRegionHeight = 250;

        public ObjectPool<Asteroid> Asteroids;
        public List<Ship> Ships;
        public Player PlayerEntity;

        //public Texture2D Planet, Background;
        public Texture2D Background;
        public Vector2 ScreenCenter;

        public BasicCamera2D Camera;
        public UnitManager UnitManager;
        GraphicsDevice Graphics;

        public WorldManager(GraphicsDevice graphics, BasicCamera2D camera, UnitManager unitManager)
        {
            UnitManager = unitManager;
            UnitManager.WorldManager = this;
            Graphics = graphics;
            Camera = camera;
            ScreenCenter = new Vector2(graphics.PresentationParameters.BackBufferWidth / 2, graphics.PresentationParameters.BackBufferHeight / 2);

            // todo - set world seed
            WorldData.RNG = new Random();

            var planet = ModManager.Instance.AssetManager.LoadTexture2D(graphics, "Planet" + WorldData.RNG.Next(1, 11).ToString(), true);
            var background = ModManager.Instance.AssetManager.LoadTexture2D(graphics, "Background" + WorldData.RNG.Next(1, 9).ToString(), true);

            Background = new RenderTarget2D(graphics, background.Width, background.Height);
            graphics.SetRenderTarget((RenderTarget2D)Background);
            using (SpriteBatch spriteBatch = new SpriteBatch(graphics))
            {
                graphics.Clear(Color.Transparent);
                spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
                spriteBatch.Draw(background, Vector2.Zero, Color.White);
                spriteBatch.Draw(planet,
                    new Vector2()
                    {
                        X = background.Width / 2 - planet.Width / 2,
                        Y = background.Height / 2 - planet.Height / 2
                    },
                    Color.White);
                spriteBatch.End();
            }
            graphics.SetRenderTarget(null);

            Asteroids = new ObjectPool<Asteroid>(10000);
            Ships = new List<Ship>();

            // random asteroid field generation
            for (var x = AsteroidRegionWidth; x < (WorldWidth - AsteroidRegionWidth); x += AsteroidRegionWidth)
            {
                for (var y = AsteroidRegionHeight; y < (WorldHeight - AsteroidRegionHeight); y += AsteroidRegionHeight)
                {
                    if (WorldData.RNG.Next(0, 10) < 2 && Asteroids.LastActiveIndex < (Asteroids.Size - 1))
                    {
                        var newAsteroid = Asteroids.New();
                        newAsteroid.Sprite = TexturePacker.GetSprite("AsteroidsAtlas", "Asteroid" + WorldData.RNG.Next(1, WorldData.AsteroidTypes + 1).ToString());
                        newAsteroid.Position = new Vector2(x + WorldData.RNG.Next(0, 150), y + WorldData.RNG.Next(0, 150));
                        newAsteroid.Origin = new Vector2(newAsteroid.Sprite.SourceRect.Width / 2, newAsteroid.Sprite.SourceRect.Height / 2);
                        newAsteroid.RotationSpeed = (float)PandaUtil.RandomDouble(WorldData.RNG, 0.0, 0.1);

                        if (WorldData.RNG.Next(0, 10) < 3)
                        {
                            newAsteroid.ResourceType = WorldData.ResourceTypes[WorldData.RNG.Next(0, WorldData.ResourceTypes.Count)];
                            newAsteroid.ResourceCount = WorldData.RNG.Next(50000, 100000);
                        }
                    }
                }
            }

            PlayerEntity = new Player();
            PlayerEntity.Position = new Vector2(500, 500);

            for (var i = 0; i < StartingMiners; i++)
            {
                UnitManager.SpawnShip(ShipType.Miner, PlayerEntity.Position + new Vector2(WorldData.RNG.Next(-200, 200), WorldData.RNG.Next(-200, 200)), PlayerEntity);
            }
        }

        ~WorldManager()
        {
            Background.Dispose();
        }

        public void Update(GameTime gameTime)
        {
            for (var i = 0; i <= Asteroids.LastActiveIndex; i++)
            {
                Asteroids[i].Update(gameTime);
            }

            for (var i = 0; i < Ships.Count; i++)
            {
                //Ships[i].SetDestination(PlayerEntity.Position);
                Ships[i].Update(gameTime);
            }
            
            PlayerEntity.Update(gameTime);
        }

        public void DrawWorld (SpriteBatch spriteBatch)
        {
            var camPos = Camera.GetPosition();
            var viewDistance = ((Graphics.PresentationParameters.BackBufferWidth * 1.1) / Camera.Zoom);

            for (var i = 0; i <= Asteroids.LastActiveIndex; i++)
            {
                var asteroid = Asteroids[i];
                var distance = Vector2.Distance(asteroid.Position, camPos);

                // quick hack to do view culling
                if (distance < viewDistance)
                    asteroid.Draw(spriteBatch);
            }

            for (var i = 0; i < Ships.Count; i++)
            {
                var ship = Ships[i];
                var distance = Vector2.Distance(ship.Position, camPos);

                // quick hack to do view culling
                if (distance < viewDistance)
                    ship.Draw(spriteBatch);
            }

            PlayerEntity.Draw(spriteBatch);
        }

        public void DrawScreen(SpriteBatch spriteBatch)
        {
            //var worldSize = new Vector2(WorldWidth * 1.5f, WorldHeight * 1.5f);
            var worldSize = new Vector2(WorldWidth, WorldHeight);
            var bgSize = new Vector2(Background.Width, Background.Height);
            var bgProportionalSize = (float)bgSize.X / (float)worldSize.X;
            float bgZoom = 1.0f - ((1.0f - Camera.Zoom) * bgProportionalSize);

            var screenPosWorld = Camera.ScreenToWorldPosition(Vector2.Zero);

            var backgroundPos = ((screenPosWorld / worldSize) * bgSize) * bgZoom;

            spriteBatch.Draw(
                        Background,
                        -backgroundPos,
                        null,
                        Color.White,
                        0.0f,
                        Vector2.Zero,
                        bgZoom,
                        SpriteEffects.None,
                        0.0f
                        );
        }
    }
}
