using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public static class RandomUtil
{
    private static Random random = new Random();
    public static char GetRandomAlphabet()
    {
        return (char)random.Next('A', 'Z' + 1);
    }
    public static char GetRandomDigit()
    {
        return (char)random.Next('0', '9' + 1);
    }
    public static char GetRandomAlphabetOrDigit()
    {
        return random.Next(0,36) < 26
            ? (char)random.Next('A', 'Z' + 1)
            : (char)random.Next('0', '9' + 1);
    }
    public static T[] GetSortedRandomSubset<T>(T[] array, int count)
    {
        if (count > array.Length)
            throw new ArgumentException("뽑으려는 개수가 배열보다 많음");

        int[] indices = new int[array.Length];
        for (int i = 0; i < array.Length; i++)
            indices[i] = i;

        // Fisher–Yates Shuffle (인덱스 셔플)
        for (int i = indices.Length - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            (indices[i], indices[j]) = (indices[j], indices[i]);
        }

        // 앞 a개만 정렬
        Array.Sort(indices, 0, count);

        // 결과 배열 생성
        T[] result = new T[count];
        for (int i = 0; i < count; i++)
            result[i] = array[indices[i]];

        return result;
    }
    public static T[] GetRandomSubset<T>(T[] array, int count)
    {
        if (array == null)
            throw new ArgumentNullException(nameof(array));
        if (count < 0 || count > array.Length)
            throw new ArgumentOutOfRangeException(nameof(count), "count는 0 이상 array.Length 이하이어야 합니다.");

        // 원본을 건드리지 않기 위해 복사본 생성
        T[] buffer = (T[])array.Clone();
        int n = buffer.Length;

        // 앞에서부터 count개만 부분 셔플
        for (int i = 0; i < count; i++)
        {
            // i 이상 n-1 사이에서 랜덤 인덱스 선택
            int j = random.Next(i, n);
            // swap buffer[i] <-> buffer[j]
            (buffer[i], buffer[j]) = (buffer[j], buffer[i]);
        }

        // 셔플된 앞 count개를 결과로 잘라서 반환
        T[] result = new T[count];
        Array.Copy(buffer, 0, result, 0, count);
        return result;
    }
    public static T[] GetShuffled<T>(T[] array)
    {
        if (array == null) 
            throw new ArgumentNullException(nameof(array));

        // 복사본 생성
        T[] result = (T[])array.Clone();
        int n = result.Length;

        // Fisher–Yates 셔플 (뒤에서부터)
        for (int i = n - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            (result[i], result[j]) = (result[j], result[i]);
        }

        return result;
    }
    public static List<T> GetShuffled<T>(List<T> list)
    {
        return GetShuffled<T>(list.ToArray()).ToList();
    }

    public static T GetRandom<T>(this List<T> list)
    {
        return GetShuffled(list)[0];
    }
}