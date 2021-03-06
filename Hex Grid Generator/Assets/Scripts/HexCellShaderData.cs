﻿using UnityEngine;

public class HexCellShaderData : MonoBehaviour
{
	Texture2D cellTexture;

	/*
		Default uncompressed RGBA textures contain pixels that are four bytes in size.
		Each of the four color channels get one byte, so they have 256 possible values.
		When using Unity's Color struct, its floating-point components in the range 0–1 are converted to bytes in the range 0–255.
		The GPU performs the reverse conversion when sampling.

		The Color32 struct works directly with bytes.
		So they take less space and don't require a conversion, which makes them more efficient to use.
		As we're storing cell data instead of colors, it also makes more sense to work directly with
		the raw texture data instead of going through Color.
	*/
	Color32[] cellTextureData;

	void LateUpdate()
	{
		cellTexture.SetPixels32(cellTextureData);
		cellTexture.Apply();
		enabled = false;
	}

	/*
	 * Whenever a new map is created or loaded, we have to create a new texture with the correct size.
	 * So give it an initialization method which creates the texture. We'll use an RGBA texture, without mipmaps,
	 * and in linear color space. We don't want to blend cell data, so use point filtering.
	 * Also, the data shouldn't wrap. Each pixel of the texture will hold the data of one cell.
	 */
	public void Initialize(int x, int z)
	{
		if (cellTexture)
		{
			cellTexture.Resize(x, z);
		}
		else
		{
			cellTexture = new Texture2D(x, z, TextureFormat.RGBA32, false, true);
			cellTexture.filterMode = FilterMode.Point;
			cellTexture.wrapMode = TextureWrapMode.Clamp;

			// Make the cell data texture globally available to all shaders.
			// This is convenient, as we'll be needing it in multiple shaders.
			Shader.SetGlobalTexture("_HexCellData", cellTexture);
		}

		/*
		 * When using a shader property, Unity also makes a texture's size available to the shader via a textureName_TexelSize variable.
		 * This is a four-component vector which contains the multiplicative inverses of the width and height,
		 * and the actual width and height. But when setting a texture globally, this is not done.
		 * So let's do it ourselves, via Shader.SetGlobalVector after the texture has been created or resized.
		 */
		Shader.SetGlobalVector("_HexCellData_TexelSize", new Vector4(1f / x, 1f / z, x, z));

		// apply all pixels data in one go
		if (cellTextureData == null || cellTextureData.Length != x * z)
		{
			cellTextureData = new Color32[x * z];
		}
		else
		{
			for (int i = 0; i < cellTextureData.Length; i++)
				cellTextureData[i] = new Color32(0, 0, 0, 0);
		}

		enabled = true;
	}

	public void RefreshTerrain(HexCell cell)
	{
		cellTextureData[cell.Index].a = (byte)cell.TerrainType;
		enabled = true;
	}

    // Store the data in the R component of the cell data. 
    // Because we're working with bytes that get converted to 0–1 values in the shader, 
    // use (byte)255 to represent visible.
    public void RefreshVisibility(HexCell cell)
    {
        cellTextureData[cell.Index].r = cell.IsVisible ? (byte)255 : (byte)0;
        enabled = true;
    }
}