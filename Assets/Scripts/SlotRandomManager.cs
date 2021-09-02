using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public class RandomSequenceData
{
    public int floor;
    public int ceiling;
    public int sequenceIndex;
    public int randomSeqIndex;
}

[System.Serializable]
public class SlotRandomManager
{
    [SerializeField] private List<SymbolSequenceProbabilityData> sequences;
    [SerializeField] private List<RandomSequenceData> randomSequenceSlots;
    private List<RandomSequenceData> emptySlots;
    private List<RandomSequenceData> remainingSequences;
    private const int emptyNumber = -1;
    private int currentRandomSeqIndex;
    private Action onRandomSeqLimitReached;

    public SlotRandomManager(List<SymbolSequenceProbabilityData> sequences, List<RandomSequenceData> randomSeqSlots = null, int currentIndex = 0)
    {
        if(sequences == null) throw new Exception("No Sequence For Random Manager");
        if (GetPercentage(sequences) != 100) throw new Exception("Total Percentage Is Not 100");

        onRandomSeqLimitReached = Calculate100RandomSequence;
        this.sequences = sequences.ConvertAll<SymbolSequenceProbabilityData>(sequence => new SymbolSequenceProbabilityData(sequence));
        this.currentRandomSeqIndex = currentIndex;

        if(randomSeqSlots != null)
        {
            this.randomSequenceSlots = new List < RandomSequenceData > (randomSeqSlots);
        }
    }

    private int GetPercentage(List<SymbolSequenceProbabilityData> sequences)
    {
        if (sequences == null) return 0;

        int percentage = 0;
        for (int i = 0; i < sequences.Count; i++)
        {
            if(sequences[i] != null)
            {
                percentage += sequences[i].percentage;
            }
        }

        return percentage;
    }

    private void ClearOrCreateList<T>(ref List<T> list, int initialSize = 20)
    {
        if (list == null)
        {
            list = new List<T>(initialSize);
        }
        else
        {
            list.Clear();
        }
    }

    private void AddEmptySlotData(int amount, params List<RandomSequenceData>[] arrays)
    {
        for (int i = 0; i < amount; i++)
        {
            RandomSequenceData empty = new RandomSequenceData();
            empty.randomSeqIndex = i;
            empty.sequenceIndex = emptyNumber;
            for (int j = 0; j < arrays.Length; j++)
            {
                if(arrays[j] != null)
                {
                    arrays[j].Add(empty);
                }
            }
        }
    }

    private void CalculateIntervals(List<int> intervals, int percentage)
    {
        intervals.Clear();
        int flooredInterval = Mathf.FloorToInt(100.0f / (float)(percentage));
        for (int i = 0; i < percentage; i++)
        {
            intervals.Add(flooredInterval);
        }
        int remnant = 100 - flooredInterval * percentage;
        for (int i = 0; i < percentage && remnant > 0; i++)
        {
            intervals[i]++;
            remnant--;
        }
    }

    private void GetEmptySlotsInInterval(List<RandomSequenceData> listToIterate, List<int> outlistToFill, int floor, int ceiling)
    {
        outlistToFill.Clear();
        for (int x = floor; x <= ceiling; x++)
        {
            if (listToIterate[x].sequenceIndex == emptyNumber)
            {
                outlistToFill.Add(x);
            }
        }
    }

    public List<SlotSymbolTypes> PullRandomSequence()
    {
        if (sequences == null) return null;

        if (randomSequenceSlots == null)
        {
            Calculate100RandomSequence();
            currentRandomSeqIndex = 0;
        }
        else if (currentRandomSeqIndex >= randomSequenceSlots.Count)
        {
            onRandomSeqLimitReached.Invoke();
            currentRandomSeqIndex = 0;
        }

        if(currentRandomSeqIndex < randomSequenceSlots.Count)
        {
            RandomSequenceData seq = randomSequenceSlots[currentRandomSeqIndex];
            if(seq.sequenceIndex < sequences.Count)
            {
                currentRandomSeqIndex++;
                return sequences[seq.sequenceIndex].symbols;
            }
        }

        return null;
    }
    public void Calculate100RandomSequence()
    {
        List<int> emptyIndices = null;
        List<int> intervals = null;

        ClearOrCreateList<RandomSequenceData>(ref randomSequenceSlots, 100);
        ClearOrCreateList<RandomSequenceData>(ref emptySlots);
        ClearOrCreateList<RandomSequenceData>(ref remainingSequences);
        ClearOrCreateList<int>(ref emptyIndices, 100);
        ClearOrCreateList<int>(ref intervals);

        AddEmptySlotData(100, randomSequenceSlots, emptySlots);

        sequences.Sort();


        for (int j = 0; j < sequences.Count; j++)
        {
            CalculateIntervals(intervals, sequences[j].percentage);
            Shuffle(intervals);

            int floor = 0;
            for (int intervalIndex = 0; intervalIndex < intervals.Count; intervalIndex++)
            {
                int ceiling = Mathf.Min((floor + intervals[intervalIndex]) - 1, randomSequenceSlots.Count - 1);

                GetEmptySlotsInInterval(randomSequenceSlots, emptyIndices, floor, ceiling);

                int randomEmptyIndex = UnityEngine.Random.Range(0, emptyIndices.Count);

                //print(string.Format("floor{0}  ceiling{1}", floor, ceiling));
                //print(string.Format("randomEmptyIndex{0}  selectedIndices.Count{1}", randomEmptyIndex, selectedIndices.Count));


                if (emptyIndices.Count <= 0)
                {
                    RandomSequenceData seq = new RandomSequenceData();
                    seq.floor = floor;
                    seq.ceiling = ceiling;
                    seq.sequenceIndex = j;
                    remainingSequences.Add(seq);

                    //Debug.Log(string.Format("floor{0}  ceiling{1}", floor, ceiling) + "COUNTDT FIND ONE " + j);
                }
                else if (emptyIndices[randomEmptyIndex] < randomSequenceSlots.Count)
                {
                    randomSequenceSlots[emptyIndices[randomEmptyIndex]].floor = floor;
                    randomSequenceSlots[emptyIndices[randomEmptyIndex]].ceiling = ceiling;
                    randomSequenceSlots[emptyIndices[randomEmptyIndex]].sequenceIndex = j;

                    emptySlots.Remove(randomSequenceSlots[emptyIndices[randomEmptyIndex]]);
                }

                floor += intervals[intervalIndex];
            }

           //yield return new WaitForSeconds(0.001f);
        }

        PlaceRemainings();

        for (int i = 0; i < randomSequenceSlots.Count; i++)
        {
            if (!(randomSequenceSlots[i].floor <= i && randomSequenceSlots[i].ceiling >= i))
            {
                Debug.Log("WRONG " + i);
            }
        }
    }

    private void PlaceRemainings()
    {
        if (emptySlots.Count == remainingSequences.Count)
        {
            for (int i = 0; i < remainingSequences.Count; i++)
            {
                RandomSequenceData emptySec = emptySlots[0];
                emptySlots.RemoveAt(0);

                if (emptySec.randomSeqIndex > remainingSequences[i].ceiling)
                {
                    for (int j = emptySec.randomSeqIndex; j >= 0; j--)
                    {
                        if (j < emptySec.randomSeqIndex && randomSequenceSlots[j].floor < emptySec.randomSeqIndex && randomSequenceSlots[j].ceiling >= emptySec.randomSeqIndex)
                        {
                            randomSequenceSlots[emptySec.randomSeqIndex] = randomSequenceSlots[j];
                            randomSequenceSlots[emptySec.randomSeqIndex].randomSeqIndex = emptySec.randomSeqIndex;

                            randomSequenceSlots[j] = emptySec;
                            emptySec.randomSeqIndex = j;

                            if (j >= remainingSequences[i].floor && j <= remainingSequences[i].ceiling)
                            {
                                //swap remaning with empty
                                remainingSequences[i].randomSeqIndex = j;
                                randomSequenceSlots[j] = remainingSequences[i];
                                break;
                            }
                        }
                    }
                }
                else if (emptySec.randomSeqIndex < remainingSequences[i].floor)
                {
                    for (int j = emptySec.randomSeqIndex; j < randomSequenceSlots.Count; j++)
                    {
                        if (j > emptySec.randomSeqIndex && randomSequenceSlots[j].floor <= emptySec.randomSeqIndex && randomSequenceSlots[j].ceiling > emptySec.randomSeqIndex)
                        {
                            randomSequenceSlots[emptySec.randomSeqIndex] = randomSequenceSlots[j];
                            randomSequenceSlots[emptySec.randomSeqIndex].randomSeqIndex = emptySec.randomSeqIndex;

                            randomSequenceSlots[j] = emptySec;
                            emptySec.randomSeqIndex = j;

                            if (j >= remainingSequences[i].floor && j <= remainingSequences[i].ceiling)
                            {
                                //swap remaning with empty
                                remainingSequences[i].randomSeqIndex = j;
                                randomSequenceSlots[j] = remainingSequences[i];
                                break;
                            }
                        }
                    }
                }
            }
        }
    }

    private void Shuffle<T>( IList<T> list)
    {
        System.Random rng = new System.Random();
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }

    //private void Update()
    //{
    //    if (Input.GetKeyDown(KeyCode.A))
    //    {
    //        //StartCoroutine(Calculate100Pulls());
    //        Calculate100RandomSequence();
    //    }
    //}
}

