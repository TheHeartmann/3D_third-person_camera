using System;
using System.Collections.Generic;

	public static class LinqExtensionUtils
	{

		//Based on this Q&A: stackoverflow.com/questions/11883469/takewhile-but-get-the-element-that-stopped-it-also
		public static IEnumerable<T> TakeWhileInclusive<T>(this IEnumerable<T> data, Func<T, bool> predicate) {
			foreach (var item in data) {
				yield return item;
				if (!predicate(item))
					break;
			}
		}
	}