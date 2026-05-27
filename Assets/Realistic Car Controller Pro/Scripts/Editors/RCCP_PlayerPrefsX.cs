//----------------------------------------------
//        Realistic Car Controller Pro
//
// Copyright 2014 - 2025 BoneCracker Games
// https://www.bonecrackergames.com
// Ekrem Bugra Ozdoganlar
//
//----------------------------------------------

// ArrayPrefs2 v 1.4

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Extended PlayerPrefs utility class supporting arrays and additional data types.
/// Provides methods to save and load bool, Vector2, Vector3, Quaternion, Color, and arrays.
/// Based on ArrayPrefs2 v1.4.
/// </summary>
public class RCCP_PlayerPrefsX {

    static private int endianDiff1;
    static private int endianDiff2;
    static private int idx;
    static private byte[] byteBlock;

    /// <summary>
    /// Supported array data types for PlayerPrefs serialization.
    /// </summary>
    enum ArrayType { Float, Int32, Bool, String, Vector2, Vector3, Quaternion, Color }

    /// <summary>
    /// Stores a boolean value in PlayerPrefs as an integer (1 = true, 0 = false).
    /// </summary>
    /// <param name="name">The PlayerPrefs key.</param>
    /// <param name="value">The boolean value to store.</param>
    /// <returns>True if the value was saved successfully, false if an exception occurred.</returns>
    public static bool SetBool(String name, bool value) {
        try {
            PlayerPrefs.SetInt(name, value ? 1 : 0);
        }
        catch {
            return false;
        }
        return true;
    }

    /// <summary>
    /// Retrieves a boolean value from PlayerPrefs.
    /// </summary>
    /// <param name="name">The PlayerPrefs key.</param>
    /// <returns>True if the stored integer value is 1, false otherwise.</returns>
    public static bool GetBool(String name) {
        return PlayerPrefs.GetInt(name) == 1;
    }

    /// <summary>
    /// Retrieves a boolean value from PlayerPrefs, returning a default value if the key does not exist.
    /// </summary>
    /// <param name="name">The PlayerPrefs key.</param>
    /// <param name="defaultValue">The default value to return if the key is not found.</param>
    /// <returns>The stored boolean value, or the default value if the key does not exist.</returns>
    public static bool GetBool(String name, bool defaultValue) {
        return (1 == PlayerPrefs.GetInt(name, defaultValue ? 1 : 0));
    }

    /// <summary>
    /// Retrieves a 64-bit long integer from PlayerPrefs by combining two 32-bit int keys.
    /// </summary>
    /// <param name="key">The base PlayerPrefs key (appends _lowBits and _highBits).</param>
    /// <param name="defaultValue">The default value to return if the key is not found.</param>
    /// <returns>The stored long value, or the default value if the key does not exist.</returns>
    public static long GetLong(string key, long defaultValue) {
        int lowBits, highBits;
        SplitLong(defaultValue, out lowBits, out highBits);
        lowBits = PlayerPrefs.GetInt(key + "_lowBits", lowBits);
        highBits = PlayerPrefs.GetInt(key + "_highBits", highBits);

        // unsigned, to prevent loss of sign bit.
        ulong ret = (uint)highBits;
        ret = (ret << 32);
        return (long)(ret | (ulong)(uint)lowBits);
    }

    /// <summary>
    /// Retrieves a 64-bit long integer from PlayerPrefs.
    /// </summary>
    /// <param name="key">The base PlayerPrefs key (appends _lowBits and _highBits).</param>
    /// <returns>The stored long value, or 0 if the key does not exist.</returns>
    public static long GetLong(string key) {
        int lowBits = PlayerPrefs.GetInt(key + "_lowBits");
        int highBits = PlayerPrefs.GetInt(key + "_highBits");

        // unsigned, to prevent loss of sign bit.
        ulong ret = (uint)highBits;
        ret = (ret << 32);
        return (long)(ret | (ulong)(uint)lowBits);
    }

    private static void SplitLong(long input, out int lowBits, out int highBits) {
        // unsigned everything, to prevent loss of sign bit.
        lowBits = (int)(uint)(ulong)input;
        highBits = (int)(uint)(input >> 32);
    }

    /// <summary>
    /// Stores a 64-bit long integer in PlayerPrefs by splitting it into two 32-bit int keys.
    /// </summary>
    /// <param name="key">The base PlayerPrefs key (appends _lowBits and _highBits).</param>
    /// <param name="value">The long value to store.</param>
    public static void SetLong(string key, long value) {
        int lowBits, highBits;
        SplitLong(value, out lowBits, out highBits);
        PlayerPrefs.SetInt(key + "_lowBits", lowBits);
        PlayerPrefs.SetInt(key + "_highBits", highBits);
    }

    /// <summary>
    /// Stores a Vector2 in PlayerPrefs as a float array.
    /// </summary>
    /// <param name="key">The PlayerPrefs key.</param>
    /// <param name="vector">The Vector2 to store.</param>
    /// <returns>True if saved successfully.</returns>
    public static bool SetVector2(String key, Vector2 vector) {
        return SetFloatArray(key, new float[] { vector.x, vector.y });
    }

    static Vector2 GetVector2(String key) {
        var floatArray = GetFloatArray(key);
        if (floatArray.Length < 2) {
            return Vector2.zero;
        }
        return new Vector2(floatArray[0], floatArray[1]);
    }

    /// <summary>
    /// Retrieves a Vector2 from PlayerPrefs, returning a default value if the key does not exist.
    /// </summary>
    /// <param name="key">The PlayerPrefs key.</param>
    /// <param name="defaultValue">The default Vector2 to return if the key is not found.</param>
    /// <returns>The stored Vector2, or the default value.</returns>
    public static Vector2 GetVector2(String key, Vector2 defaultValue) {
        if (PlayerPrefs.HasKey(key)) {
            return GetVector2(key);
        }
        return defaultValue;
    }

    /// <summary>
    /// Stores a Vector3 in PlayerPrefs as a float array.
    /// </summary>
    /// <param name="key">The PlayerPrefs key.</param>
    /// <param name="vector">The Vector3 to store.</param>
    /// <returns>True if saved successfully.</returns>
    public static bool SetVector3(String key, Vector3 vector) {
        return SetFloatArray(key, new float[] { vector.x, vector.y, vector.z });
    }

    /// <summary>
    /// Retrieves a Vector3 from PlayerPrefs.
    /// </summary>
    /// <param name="key">The PlayerPrefs key.</param>
    /// <returns>The stored Vector3, or Vector3.zero if not found or corrupt.</returns>
    public static Vector3 GetVector3(String key) {
        var floatArray = GetFloatArray(key);
        if (floatArray.Length < 3) {
            return Vector3.zero;
        }
        return new Vector3(floatArray[0], floatArray[1], floatArray[2]);
    }

    /// <summary>
    /// Retrieves a Vector3 from PlayerPrefs, returning a default value if the key does not exist.
    /// </summary>
    /// <param name="key">The PlayerPrefs key.</param>
    /// <param name="defaultValue">The default Vector3 to return if the key is not found.</param>
    /// <returns>The stored Vector3, or the default value.</returns>
    public static Vector3 GetVector3(String key, Vector3 defaultValue) {
        if (PlayerPrefs.HasKey(key)) {
            return GetVector3(key);
        }
        return defaultValue;
    }

    /// <summary>
    /// Stores a Quaternion in PlayerPrefs as a float array.
    /// </summary>
    /// <param name="key">The PlayerPrefs key.</param>
    /// <param name="vector">The Quaternion to store.</param>
    /// <returns>True if saved successfully.</returns>
    public static bool SetQuaternion(String key, Quaternion vector) {
        return SetFloatArray(key, new float[] { vector.x, vector.y, vector.z, vector.w });
    }

    /// <summary>
    /// Retrieves a Quaternion from PlayerPrefs.
    /// </summary>
    /// <param name="key">The PlayerPrefs key.</param>
    /// <returns>The stored Quaternion, or Quaternion.identity if not found or corrupt.</returns>
    public static Quaternion GetQuaternion(String key) {
        var floatArray = GetFloatArray(key);
        if (floatArray.Length < 4) {
            return Quaternion.identity;
        }
        return new Quaternion(floatArray[0], floatArray[1], floatArray[2], floatArray[3]);
    }

    /// <summary>
    /// Retrieves a Quaternion from PlayerPrefs, returning a default value if the key does not exist.
    /// </summary>
    /// <param name="key">The PlayerPrefs key.</param>
    /// <param name="defaultValue">The default Quaternion to return if the key is not found.</param>
    /// <returns>The stored Quaternion, or the default value.</returns>
    public static Quaternion GetQuaternion(String key, Quaternion defaultValue) {
        if (PlayerPrefs.HasKey(key)) {
            return GetQuaternion(key);
        }
        return defaultValue;
    }

    /// <summary>
    /// Stores a Color in PlayerPrefs as a float array (RGBA).
    /// </summary>
    /// <param name="key">The PlayerPrefs key.</param>
    /// <param name="color">The Color to store.</param>
    /// <returns>True if saved successfully.</returns>
    public static bool SetColor(String key, Color color) {
        return SetFloatArray(key, new float[] { color.r, color.g, color.b, color.a });
    }

    /// <summary>
    /// Retrieves a Color from PlayerPrefs.
    /// </summary>
    /// <param name="key">The PlayerPrefs key.</param>
    /// <returns>The stored Color, or transparent black if not found or corrupt.</returns>
    public static Color GetColor(String key) {
        var floatArray = GetFloatArray(key);
        if (floatArray.Length < 4) {
            return new Color(0.0f, 0.0f, 0.0f, 0.0f);
        }
        return new Color(floatArray[0], floatArray[1], floatArray[2], floatArray[3]);
    }

    /// <summary>
    /// Retrieves a Color from PlayerPrefs, returning a default value if the key does not exist.
    /// </summary>
    /// <param name="key">The PlayerPrefs key.</param>
    /// <param name="defaultValue">The default Color to return if the key is not found.</param>
    /// <returns>The stored Color, or the default value.</returns>
    public static Color GetColor(String key, Color defaultValue) {
        if (PlayerPrefs.HasKey(key)) {
            return GetColor(key);
        }
        return defaultValue;
    }

    /// <summary>
    /// Stores a boolean array in PlayerPrefs using bit packing for efficient storage.
    /// </summary>
    /// <param name="key">The PlayerPrefs key.</param>
    /// <param name="boolArray">The boolean array to store.</param>
    /// <returns>True if saved successfully.</returns>
    public static bool SetBoolArray(String key, bool[] boolArray) {
        // Make a byte array that's a multiple of 8 in length, plus 5 bytes to store the number of entries as an int32 (+ identifier)
        // We have to store the number of entries, since the boolArray length might not be a multiple of 8, so there could be some padded zeroes
        var bytes = new byte[(boolArray.Length + 7) / 8 + 5];
        bytes[0] = System.Convert.ToByte(ArrayType.Bool);   // Identifier
        var bits = new BitArray(boolArray);
        bits.CopyTo(bytes, 5);
        Initialize();
        ConvertInt32ToBytes(boolArray.Length, bytes); // The number of entries in the boolArray goes in the first 4 bytes

        return SaveBytes(key, bytes);
    }

    /// <summary>
    /// Retrieves a boolean array from PlayerPrefs.
    /// </summary>
    /// <param name="key">The PlayerPrefs key.</param>
    /// <returns>The stored boolean array, or an empty array if the key does not exist.</returns>
    public static bool[] GetBoolArray(String key) {
        if (PlayerPrefs.HasKey(key)) {
            var bytes = System.Convert.FromBase64String(PlayerPrefs.GetString(key));
            if (bytes.Length < 5) {
                Debug.LogError("Corrupt preference file for " + key);
                return new bool[0];
            }
            if ((ArrayType)bytes[0] != ArrayType.Bool) {
                Debug.LogError(key + " is not a boolean array");
                return new bool[0];
            }
            Initialize();

            // Make a new bytes array that doesn't include the number of entries + identifier (first 5 bytes) and turn that into a BitArray
            var bytes2 = new byte[bytes.Length - 5];
            System.Array.Copy(bytes, 5, bytes2, 0, bytes2.Length);
            var bits = new BitArray(bytes2);
            // Get the number of entries from the first 4 bytes after the identifier and resize the BitArray to that length, then convert it to a boolean array
            bits.Length = ConvertBytesToInt32(bytes);
            var boolArray = new bool[bits.Count];
            bits.CopyTo(boolArray, 0);

            return boolArray;
        }
        return new bool[0];
    }

    /// <summary>
    /// Retrieves a boolean array from PlayerPrefs, returning a default-filled array if the key does not exist.
    /// </summary>
    /// <param name="key">The PlayerPrefs key.</param>
    /// <param name="defaultValue">The default value for each element.</param>
    /// <param name="defaultSize">The size of the default array.</param>
    /// <returns>The stored boolean array, or a default-filled array.</returns>
    public static bool[] GetBoolArray(String key, bool defaultValue, int defaultSize) {
        if (PlayerPrefs.HasKey(key)) {
            return GetBoolArray(key);
        }
        var boolArray = new bool[defaultSize];
        for (int i = 0; i < defaultSize; i++) {
            boolArray[i] = defaultValue;
        }
        return boolArray;
    }

    /// <summary>
    /// Stores a string array in PlayerPrefs. Each string must be 255 characters or fewer.
    /// </summary>
    /// <param name="key">The PlayerPrefs key.</param>
    /// <param name="stringArray">The string array to store (no null entries allowed).</param>
    /// <returns>True if saved successfully.</returns>
    public static bool SetStringArray(String key, String[] stringArray) {
        var bytes = new byte[stringArray.Length + 1];
        bytes[0] = System.Convert.ToByte(ArrayType.String); // Identifier
        Initialize();

        // Store the length of each string that's in stringArray, so we can extract the correct strings in GetStringArray
        for (var i = 0; i < stringArray.Length; i++) {
            if (stringArray[i] == null) {
                Debug.LogError("Can't save null entries in the string array when setting " + key);
                return false;
            }
            if (stringArray[i].Length > 255) {
                Debug.LogError("Strings cannot be longer than 255 characters when setting " + key);
                return false;
            }
            bytes[idx++] = (byte)stringArray[i].Length;
        }

        try {
            PlayerPrefs.SetString(key, System.Convert.ToBase64String(bytes) + "|" + String.Join("", stringArray));
        }
        catch {
            return false;
        }
        return true;
    }

    /// <summary>
    /// Retrieves a string array from PlayerPrefs.
    /// </summary>
    /// <param name="key">The PlayerPrefs key.</param>
    /// <returns>The stored string array, or an empty array if the key does not exist.</returns>
    public static String[] GetStringArray(String key) {
        if (PlayerPrefs.HasKey(key)) {
            var completeString = PlayerPrefs.GetString(key);
            var separatorIndex = completeString.IndexOf("|"[0]);
            if (separatorIndex < 4) {
                Debug.LogError("Corrupt preference file for " + key);
                return new String[0];
            }
            var bytes = System.Convert.FromBase64String(completeString.Substring(0, separatorIndex));
            if ((ArrayType)bytes[0] != ArrayType.String) {
                Debug.LogError(key + " is not a string array");
                return new String[0];
            }
            Initialize();

            var numberOfEntries = bytes.Length - 1;
            var stringArray = new String[numberOfEntries];
            var stringIndex = separatorIndex + 1;
            for (var i = 0; i < numberOfEntries; i++) {
                int stringLength = bytes[idx++];
                if (stringIndex + stringLength > completeString.Length) {
                    Debug.LogError("Corrupt preference file for " + key);
                    return new String[0];
                }
                stringArray[i] = completeString.Substring(stringIndex, stringLength);
                stringIndex += stringLength;
            }

            return stringArray;
        }
        return new String[0];
    }

    /// <summary>
    /// Retrieves a string array from PlayerPrefs, returning a default-filled array if the key does not exist.
    /// </summary>
    /// <param name="key">The PlayerPrefs key.</param>
    /// <param name="defaultValue">The default string for each element.</param>
    /// <param name="defaultSize">The size of the default array.</param>
    /// <returns>The stored string array, or a default-filled array.</returns>
    public static String[] GetStringArray(String key, String defaultValue, int defaultSize) {
        if (PlayerPrefs.HasKey(key)) {
            return GetStringArray(key);
        }
        var stringArray = new String[defaultSize];
        for (int i = 0; i < defaultSize; i++) {
            stringArray[i] = defaultValue;
        }
        return stringArray;
    }

    /// <summary>
    /// Stores an integer array in PlayerPrefs.
    /// </summary>
    /// <param name="key">The PlayerPrefs key.</param>
    /// <param name="intArray">The integer array to store.</param>
    /// <returns>True if saved successfully.</returns>
    public static bool SetIntArray(String key, int[] intArray) {
        return SetValue(key, intArray, ArrayType.Int32, 1, ConvertFromInt);
    }

    /// <summary>
    /// Stores a float array in PlayerPrefs.
    /// </summary>
    /// <param name="key">The PlayerPrefs key.</param>
    /// <param name="floatArray">The float array to store.</param>
    /// <returns>True if saved successfully.</returns>
    public static bool SetFloatArray(String key, float[] floatArray) {
        return SetValue(key, floatArray, ArrayType.Float, 1, ConvertFromFloat);
    }

    /// <summary>
    /// Stores a Vector2 array in PlayerPrefs.
    /// </summary>
    /// <param name="key">The PlayerPrefs key.</param>
    /// <param name="vector2Array">The Vector2 array to store.</param>
    /// <returns>True if saved successfully.</returns>
    public static bool SetVector2Array(String key, Vector2[] vector2Array) {
        return SetValue(key, vector2Array, ArrayType.Vector2, 2, ConvertFromVector2);
    }

    /// <summary>
    /// Stores a Vector3 array in PlayerPrefs.
    /// </summary>
    /// <param name="key">The PlayerPrefs key.</param>
    /// <param name="vector3Array">The Vector3 array to store.</param>
    /// <returns>True if saved successfully.</returns>
    public static bool SetVector3Array(String key, Vector3[] vector3Array) {
        return SetValue(key, vector3Array, ArrayType.Vector3, 3, ConvertFromVector3);
    }

    /// <summary>
    /// Stores a Quaternion array in PlayerPrefs.
    /// </summary>
    /// <param name="key">The PlayerPrefs key.</param>
    /// <param name="quaternionArray">The Quaternion array to store.</param>
    /// <returns>True if saved successfully.</returns>
    public static bool SetQuaternionArray(String key, Quaternion[] quaternionArray) {
        return SetValue(key, quaternionArray, ArrayType.Quaternion, 4, ConvertFromQuaternion);
    }

    /// <summary>
    /// Stores a Color array in PlayerPrefs.
    /// </summary>
    /// <param name="key">The PlayerPrefs key.</param>
    /// <param name="colorArray">The Color array to store.</param>
    /// <returns>True if saved successfully.</returns>
    public static bool SetColorArray(String key, Color[] colorArray) {
        return SetValue(key, colorArray, ArrayType.Color, 4, ConvertFromColor);
    }

    private static bool SetValue<T>(String key, T array, ArrayType arrayType, int vectorNumber, Action<T, byte[], int> convert) where T : IList {
        var bytes = new byte[(4 * array.Count) * vectorNumber + 1];
        bytes[0] = System.Convert.ToByte(arrayType);    // Identifier
        Initialize();

        for (var i = 0; i < array.Count; i++) {
            convert(array, bytes, i);
        }
        return SaveBytes(key, bytes);
    }

    private static void ConvertFromInt(int[] array, byte[] bytes, int i) {
        ConvertInt32ToBytes(array[i], bytes);
    }

    private static void ConvertFromFloat(float[] array, byte[] bytes, int i) {
        ConvertFloatToBytes(array[i], bytes);
    }

    private static void ConvertFromVector2(Vector2[] array, byte[] bytes, int i) {
        ConvertFloatToBytes(array[i].x, bytes);
        ConvertFloatToBytes(array[i].y, bytes);
    }

    private static void ConvertFromVector3(Vector3[] array, byte[] bytes, int i) {
        ConvertFloatToBytes(array[i].x, bytes);
        ConvertFloatToBytes(array[i].y, bytes);
        ConvertFloatToBytes(array[i].z, bytes);
    }

    private static void ConvertFromQuaternion(Quaternion[] array, byte[] bytes, int i) {
        ConvertFloatToBytes(array[i].x, bytes);
        ConvertFloatToBytes(array[i].y, bytes);
        ConvertFloatToBytes(array[i].z, bytes);
        ConvertFloatToBytes(array[i].w, bytes);
    }

    private static void ConvertFromColor(Color[] array, byte[] bytes, int i) {
        ConvertFloatToBytes(array[i].r, bytes);
        ConvertFloatToBytes(array[i].g, bytes);
        ConvertFloatToBytes(array[i].b, bytes);
        ConvertFloatToBytes(array[i].a, bytes);
    }

    /// <summary>
    /// Retrieves an integer array from PlayerPrefs.
    /// </summary>
    /// <param name="key">The PlayerPrefs key.</param>
    /// <returns>The stored integer array, or an empty array if the key does not exist.</returns>
    public static int[] GetIntArray(String key) {
        var intList = new List<int>();
        GetValue(key, intList, ArrayType.Int32, 1, ConvertToInt);
        return intList.ToArray();
    }

    /// <summary>
    /// Retrieves an integer array from PlayerPrefs, returning a default-filled array if the key does not exist.
    /// </summary>
    /// <param name="key">The PlayerPrefs key.</param>
    /// <param name="defaultValue">The default value for each element.</param>
    /// <param name="defaultSize">The size of the default array.</param>
    /// <returns>The stored integer array, or a default-filled array.</returns>
    public static int[] GetIntArray(String key, int defaultValue, int defaultSize) {
        if (PlayerPrefs.HasKey(key)) {
            return GetIntArray(key);
        }
        var intArray = new int[defaultSize];
        for (int i = 0; i < defaultSize; i++) {
            intArray[i] = defaultValue;
        }
        return intArray;
    }

    /// <summary>
    /// Retrieves a float array from PlayerPrefs.
    /// </summary>
    /// <param name="key">The PlayerPrefs key.</param>
    /// <returns>The stored float array, or an empty array if the key does not exist.</returns>
    public static float[] GetFloatArray(String key) {
        var floatList = new List<float>();
        GetValue(key, floatList, ArrayType.Float, 1, ConvertToFloat);
        return floatList.ToArray();
    }

    /// <summary>
    /// Retrieves a float array from PlayerPrefs, returning a default-filled array if the key does not exist.
    /// </summary>
    /// <param name="key">The PlayerPrefs key.</param>
    /// <param name="defaultValue">The default value for each element.</param>
    /// <param name="defaultSize">The size of the default array.</param>
    /// <returns>The stored float array, or a default-filled array.</returns>
    public static float[] GetFloatArray(String key, float defaultValue, int defaultSize) {
        if (PlayerPrefs.HasKey(key)) {
            return GetFloatArray(key);
        }
        var floatArray = new float[defaultSize];
        for (int i = 0; i < defaultSize; i++) {
            floatArray[i] = defaultValue;
        }
        return floatArray;
    }

    /// <summary>
    /// Retrieves a Vector2 array from PlayerPrefs.
    /// </summary>
    /// <param name="key">The PlayerPrefs key.</param>
    /// <returns>The stored Vector2 array, or an empty array if the key does not exist.</returns>
    public static Vector2[] GetVector2Array(String key) {
        var vector2List = new List<Vector2>();
        GetValue(key, vector2List, ArrayType.Vector2, 2, ConvertToVector2);
        return vector2List.ToArray();
    }

    /// <summary>
    /// Retrieves a Vector2 array from PlayerPrefs, returning a default-filled array if the key does not exist.
    /// </summary>
    /// <param name="key">The PlayerPrefs key.</param>
    /// <param name="defaultValue">The default Vector2 for each element.</param>
    /// <param name="defaultSize">The size of the default array.</param>
    /// <returns>The stored Vector2 array, or a default-filled array.</returns>
    public static Vector2[] GetVector2Array(String key, Vector2 defaultValue, int defaultSize) {
        if (PlayerPrefs.HasKey(key)) {
            return GetVector2Array(key);
        }
        var vector2Array = new Vector2[defaultSize];
        for (int i = 0; i < defaultSize; i++) {
            vector2Array[i] = defaultValue;
        }
        return vector2Array;
    }

    /// <summary>
    /// Retrieves a Vector3 array from PlayerPrefs.
    /// </summary>
    /// <param name="key">The PlayerPrefs key.</param>
    /// <returns>The stored Vector3 array, or an empty array if the key does not exist.</returns>
    public static Vector3[] GetVector3Array(String key) {
        var vector3List = new List<Vector3>();
        GetValue(key, vector3List, ArrayType.Vector3, 3, ConvertToVector3);
        return vector3List.ToArray();
    }

    /// <summary>
    /// Retrieves a Vector3 array from PlayerPrefs, returning a default-filled array if the key does not exist.
    /// </summary>
    /// <param name="key">The PlayerPrefs key.</param>
    /// <param name="defaultValue">The default Vector3 for each element.</param>
    /// <param name="defaultSize">The size of the default array.</param>
    /// <returns>The stored Vector3 array, or a default-filled array.</returns>
    public static Vector3[] GetVector3Array(String key, Vector3 defaultValue, int defaultSize) {
        if (PlayerPrefs.HasKey(key)) {
            return GetVector3Array(key);
        }
        var vector3Array = new Vector3[defaultSize];
        for (int i = 0; i < defaultSize; i++) {
            vector3Array[i] = defaultValue;
        }
        return vector3Array;
    }

    /// <summary>
    /// Retrieves a Quaternion array from PlayerPrefs.
    /// </summary>
    /// <param name="key">The PlayerPrefs key.</param>
    /// <returns>The stored Quaternion array, or an empty array if the key does not exist.</returns>
    public static Quaternion[] GetQuaternionArray(String key) {
        var quaternionList = new List<Quaternion>();
        GetValue(key, quaternionList, ArrayType.Quaternion, 4, ConvertToQuaternion);
        return quaternionList.ToArray();
    }

    /// <summary>
    /// Retrieves a Quaternion array from PlayerPrefs, returning a default-filled array if the key does not exist.
    /// </summary>
    /// <param name="key">The PlayerPrefs key.</param>
    /// <param name="defaultValue">The default Quaternion for each element.</param>
    /// <param name="defaultSize">The size of the default array.</param>
    /// <returns>The stored Quaternion array, or a default-filled array.</returns>
    public static Quaternion[] GetQuaternionArray(String key, Quaternion defaultValue, int defaultSize) {
        if (PlayerPrefs.HasKey(key)) {
            return GetQuaternionArray(key);
        }
        var quaternionArray = new Quaternion[defaultSize];
        for (int i = 0; i < defaultSize; i++) {
            quaternionArray[i] = defaultValue;
        }
        return quaternionArray;
    }

    /// <summary>
    /// Retrieves a Color array from PlayerPrefs.
    /// </summary>
    /// <param name="key">The PlayerPrefs key.</param>
    /// <returns>The stored Color array, or an empty array if the key does not exist.</returns>
    public static Color[] GetColorArray(String key) {
        var colorList = new List<Color>();
        GetValue(key, colorList, ArrayType.Color, 4, ConvertToColor);
        return colorList.ToArray();
    }

    /// <summary>
    /// Retrieves a Color array from PlayerPrefs, returning a default-filled array if the key does not exist.
    /// </summary>
    /// <param name="key">The PlayerPrefs key.</param>
    /// <param name="defaultValue">The default Color for each element.</param>
    /// <param name="defaultSize">The size of the default array.</param>
    /// <returns>The stored Color array, or a default-filled array.</returns>
    public static Color[] GetColorArray(String key, Color defaultValue, int defaultSize) {
        if (PlayerPrefs.HasKey(key)) {
            return GetColorArray(key);
        }
        var colorArray = new Color[defaultSize];
        for (int i = 0; i < defaultSize; i++) {
            colorArray[i] = defaultValue;
        }
        return colorArray;
    }

    private static void GetValue<T>(String key, T list, ArrayType arrayType, int vectorNumber, Action<T, byte[]> convert) where T : IList {
        if (PlayerPrefs.HasKey(key)) {
            var bytes = System.Convert.FromBase64String(PlayerPrefs.GetString(key));
            if ((bytes.Length - 1) % (vectorNumber * 4) != 0) {
                Debug.LogError("Corrupt preference file for " + key);
                return;
            }
            if ((ArrayType)bytes[0] != arrayType) {
                Debug.LogError(key + " is not a " + arrayType.ToString() + " array");
                return;
            }
            Initialize();

            var end = (bytes.Length - 1) / (vectorNumber * 4);
            for (var i = 0; i < end; i++) {
                convert(list, bytes);
            }
        }
    }

    private static void ConvertToInt(List<int> list, byte[] bytes) {
        list.Add(ConvertBytesToInt32(bytes));
    }

    private static void ConvertToFloat(List<float> list, byte[] bytes) {
        list.Add(ConvertBytesToFloat(bytes));
    }

    private static void ConvertToVector2(List<Vector2> list, byte[] bytes) {
        list.Add(new Vector2(ConvertBytesToFloat(bytes), ConvertBytesToFloat(bytes)));
    }

    private static void ConvertToVector3(List<Vector3> list, byte[] bytes) {
        list.Add(new Vector3(ConvertBytesToFloat(bytes), ConvertBytesToFloat(bytes), ConvertBytesToFloat(bytes)));
    }

    private static void ConvertToQuaternion(List<Quaternion> list, byte[] bytes) {
        list.Add(new Quaternion(ConvertBytesToFloat(bytes), ConvertBytesToFloat(bytes), ConvertBytesToFloat(bytes), ConvertBytesToFloat(bytes)));
    }

    private static void ConvertToColor(List<Color> list, byte[] bytes) {
        list.Add(new Color(ConvertBytesToFloat(bytes), ConvertBytesToFloat(bytes), ConvertBytesToFloat(bytes), ConvertBytesToFloat(bytes)));
    }

    /// <summary>
    /// Logs the array type stored under the given PlayerPrefs key to the Unity console.
    /// </summary>
    /// <param name="key">The PlayerPrefs key to inspect.</param>
    public static void ShowArrayType(String key) {
        var bytes = System.Convert.FromBase64String(PlayerPrefs.GetString(key));
        if (bytes.Length > 0) {
            ArrayType arrayType = (ArrayType)bytes[0];
            Debug.Log(key + " is a " + arrayType.ToString() + " array");
        }
    }

    private static void Initialize() {
        if (System.BitConverter.IsLittleEndian) {
            endianDiff1 = 0;
            endianDiff2 = 0;
        } else {
            endianDiff1 = 3;
            endianDiff2 = 1;
        }
        if (byteBlock == null) {
            byteBlock = new byte[4];
        }
        idx = 1;
    }

    private static bool SaveBytes(String key, byte[] bytes) {
        try {
            PlayerPrefs.SetString(key, System.Convert.ToBase64String(bytes));
        }
        catch {
            return false;
        }
        return true;
    }

    private static void ConvertFloatToBytes(float f, byte[] bytes) {
        byteBlock = System.BitConverter.GetBytes(f);
        ConvertTo4Bytes(bytes);
    }

    private static float ConvertBytesToFloat(byte[] bytes) {
        ConvertFrom4Bytes(bytes);
        return System.BitConverter.ToSingle(byteBlock, 0);
    }

    private static void ConvertInt32ToBytes(int i, byte[] bytes) {
        byteBlock = System.BitConverter.GetBytes(i);
        ConvertTo4Bytes(bytes);
    }

    private static int ConvertBytesToInt32(byte[] bytes) {
        ConvertFrom4Bytes(bytes);
        return System.BitConverter.ToInt32(byteBlock, 0);
    }

    private static void ConvertTo4Bytes(byte[] bytes) {
        bytes[idx] = byteBlock[endianDiff1];
        bytes[idx + 1] = byteBlock[1 + endianDiff2];
        bytes[idx + 2] = byteBlock[2 - endianDiff2];
        bytes[idx + 3] = byteBlock[3 - endianDiff1];
        idx += 4;
    }

    private static void ConvertFrom4Bytes(byte[] bytes) {
        byteBlock[endianDiff1] = bytes[idx];
        byteBlock[1 + endianDiff2] = bytes[idx + 1];
        byteBlock[2 - endianDiff2] = bytes[idx + 2];
        byteBlock[3 - endianDiff1] = bytes[idx + 3];
        idx += 4;
    }
}