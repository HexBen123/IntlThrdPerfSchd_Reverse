namespace IntlThrdPerfSchd
{
	public struct DecisionRecord
	{
		public float[] ThreadFeatures;

		public float[][] CoreFeatures;

		public int NumCores;

		public int SelectedCore;

		public float Reward;

		public long Timestamp;

		public int PredictedCore;

		public int ActualCore;

		public bool IsValid;

		public DecisionRecord(int maxCores)
		{
			ThreadFeatures = new float[8];
			CoreFeatures = new float[maxCores][];
			for (int i = 0; i < maxCores; i++)
			{
				CoreFeatures[i] = new float[9];
			}
			NumCores = 0;
			SelectedCore = -1;
			Reward = 0f;
			Timestamp = 0L;
			PredictedCore = -1;
			ActualCore = -1;
			IsValid = true;
		}
	}
}
