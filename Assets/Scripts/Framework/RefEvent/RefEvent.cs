

namespace RefEvent
{
	public delegate void RefAction<T>(ref T arg) where T : struct;
	public delegate void RefAction<T1, T2>(ref T1 t1, ref T2 t2) where T1 : struct where T2 : struct;
	public delegate void RefAction<T1, T2, T3>(ref T1 t1, ref T2 t2, ref T3 t3)
		where T1 : struct where T2 : struct where T3 : struct;
	public delegate void RefAction<T1, T2, T3, T4>(ref T1 t1, ref T2 t2, ref T3 t3, ref T4 t4)
		where T1 : struct where T2 : struct where T3 : struct where T4 : struct;
	public delegate void RefAction<T1, T2, T3, T4, T5>(ref T1 t1, ref T2 t2, ref T3 t3, ref T4 t4, ref T5 t5) where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct;
	public delegate void RefAction<T1, T2, T3, T4, T5, T6>(ref T1 t1, ref T2 t2, ref T3 t3, ref T4 t4, ref T5 t5, ref T6 t6) where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct where T6 : struct;


}
