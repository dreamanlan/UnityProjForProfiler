using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.MemoryProfilerExtension.Editor.EnumerationUtilities
{
    public class EnumerationStatus
    {
        public int StepCount { private set; get; }
        public int CurrentStep { private set; get; }
        public string StepStatus;

        public EnumerationStatus(int steps)
        {
            StepCount = steps;
            CurrentStep = 0;
        }

        public EnumerationStatus IncrementStep(string stepStatus = null)
        {
            if (CurrentStep + 1 == StepCount)
            {
                Debug.LogError("Failed to increment step as it would exceed maximum step count");
                return null;
            }
            ++CurrentStep;
            if (stepStatus != null)
                StepStatus = stepStatus;
            return this;
        }
    }
}
