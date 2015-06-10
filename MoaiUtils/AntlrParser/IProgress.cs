namespace CppParser {

	public interface IProgress {
		double GetProgress();
	}

	public class IntProgress : IProgress {
		public int MaxValue { get; set; }
		public int Value { get; set; }

		public double GetProgress() {
			return MaxValue != 0
				? ((double) Value)/MaxValue
				: 0;
		}
	}

}