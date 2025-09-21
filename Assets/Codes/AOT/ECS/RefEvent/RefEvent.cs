

namespace RefEvent
{
	public delegate void RefAction<T>(ref T arg);
	public delegate void RefAction<T1, T2>(ref T1 t1, ref T2 t2);
	public delegate void RefAction<T1, T2, T3>(ref T1 t1, ref T2 t2, ref T3 t3);
	public delegate void RefAction<T1, T2, T3, T4>(ref T1 t1, ref T2 t2, ref T3 t3, ref T4 t4);
	public delegate void RefAction<T1, T2, T3, T4, T5>(ref T1 t1, ref T2 t2, ref T3 t3, ref T4 t4, ref T5 t5);
	public delegate void RefAction<T1, T2, T3, T4, T5, T6>(ref T1 t1, ref T2 t2, ref T3 t3, ref T4 t4, ref T5 t5, ref T6 t6);

}
