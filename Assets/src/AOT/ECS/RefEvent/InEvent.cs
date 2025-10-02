
namespace inEvent
{
	public delegate void InAction<T>(in T arg);
	public delegate void InAction<T1, T2>(in T1 t1, in T2 t2);
	public delegate void InAction<T1, T2, T3>(in T1 t1, in T2 t2, in T3 t3);
	public delegate void InAction<T1, T2, T3, T4>(in T1 t1, in T2 t2, in T3 t3, in T4 t4);
	public delegate void InAction<T1, T2, T3, T4, T5>(in T1 t1, in T2 t2, in T3 t3, in T4 t4, in T5 t5);
	public delegate void InAction<T1, T2, T3, T4, T5, T6>(in T1 t1, in T2 t2, in T3 t3, in T4 t4, in T5 t5, in T6 t6);
}
