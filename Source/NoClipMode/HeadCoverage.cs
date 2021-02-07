﻿using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace ShowHair.NoClipMode
{
	public interface IHeadCoverage
	{
		string GetTexPath(Pawn pawn, Enums.Coverage coverage);
	}

	public static class HeadCoverage
	{
		//nothing covering head
		public class NotCovered : IHeadCoverage
		{
			public string GetTexPath(Pawn pawn, Enums.Coverage coverage)
			{
				//return normal hair
				return pawn.story.hairDef.texPath;
			}
		}

		//head covered, but not using fallback textures
		public class Covered : IHeadCoverage
		{
			public string GetTexPath(Pawn pawn, Enums.Coverage coverage)
			{
				// Check if custom texture path exists
				if (!ContentFinder<Texture2D>.Get($"{pawn.story.hairDef.texPath}/{coverage}_south", false))
				{
					//if no custom texture return shaved i.e. default rimworld
					return HairDefOf.Shaved.texPath;
				}

				return $"{pawn.story.hairDef.texPath}/{coverage}";
			}
		}

		//head covered, using fallback textures
		public class Covered_Fallback : IHeadCoverage
		{
			private readonly Dictionary<string, string> cachedTextures; //keep a cache of already used texture combos, so we don't have to keep calculating them

			private List<FallbackTextureListDef> _fallbackTexturesList;
			private List<FallbackTextureListDef> fallbackTexturesList //textures list from Defs/FallbackTextureList.xml
			{
				get
				{
					if(this._fallbackTexturesList == null)
					{
						this._fallbackTexturesList = DefDatabase<FallbackTextureListDef>.AllDefs.ToList();
					}
					return this._fallbackTexturesList;
				}
			}

			public Covered_Fallback()
			{
				this.cachedTextures = new Dictionary<string, string>();
			}

			public string GetTexPath(Pawn pawn, Enums.Coverage coverage)
			{
				// get current hair path
				var texPath = pawn.story.hairDef.texPath;

				if (this.cachedTextures.ContainsKey(texPath))
				{
					//get texture from cache if it already exists
					texPath = this.cachedTextures[texPath];

					//Log.Message($"{pawn.Name} | retreived from cache | {texPath}");
				}
				else
				{
					// Check if custom texture path exists
					if (!ContentFinder<Texture2D>.Get($"{texPath}/{coverage}_south", false))//couldn't find a custom texture, get a semi-random fallback
					{
						//get lowest pixel to estimate hair length
						int bottomPixel = TextureUtility.GetBottomPixelPercentage(pawn, texPath, Rot4.East);

						//get the fallback textures for the pixel range
						var textures = this.fallbackTexturesList.Where(ft => bottomPixel >= ft.bottomPixelRange.start && bottomPixel <= ft.bottomPixelRange.end).FirstOrDefault();

						string closestFallbackPath = textures.GetClostestFallbackTexturePath(texPath);

						//Log.Message($"{pawn.Name} | {bottomPixel} | {texPath} | {closestFallbackPath}");

						//adding to the cache so we don't have to do the lookup again
						this.cachedTextures.Add(texPath, closestFallbackPath);

						texPath = closestFallbackPath;
					}
				}

				return $"{texPath}/{coverage}";
			}
		}
	}
}
