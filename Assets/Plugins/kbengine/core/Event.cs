namespace KBEngine
{
  	using UnityEngine; 
	using System; 
	using System.Collections; 
	using System.Collections.Generic;
	using System.Threading;
	
    public class Event
    {
		public struct Pair
		{
			public object obj;
			public string funcname;
			public System.Reflection.MethodInfo method;
		};
		
		public struct EventObj
		{
			public Pair info;
			public object[] args;
		};
		
    	public static Dictionary<string, List<Pair>> events_out = new Dictionary<string, List<Pair>>();
		
		public static List<EventObj> firedEvents_out = new List<EventObj>();
		private static List<EventObj> doingEvents_out = new List<EventObj>();
		
    	public static Dictionary<string, List<Pair>> events_in = new Dictionary<string, List<Pair>>();
		
		public static List<EventObj> firedEvents_in = new List<EventObj>();
		private static List<EventObj> doingEvents_in = new List<EventObj>();
		
		public Event()
		{
		}
		
		public static bool registerOut(string eventname, object obj, string funcname)
		{
			return register(events_out, eventname, obj, funcname);
		}

		public static bool registerIn(string eventname, object obj, string funcname)
		{
			return register(events_in, eventname, obj, funcname);
		}
		
		private static bool register(Dictionary<string, List<Pair>> events, string eventname, object obj, string funcname)
		{
			deregister(events, eventname, obj, funcname);
			List<Pair> lst = null;
			
			Pair pair = new Pair();
			pair.obj = obj;
			pair.funcname = funcname;
			pair.method = obj.GetType().GetMethod(funcname);
			if(pair.method == null)
			{
				Dbg.ERROR_MSG("Event::register: " + obj + "not found method[" + funcname + "]");
				return false;
			}
			
			Monitor.Enter(events);
			if(!events.TryGetValue(eventname, out lst))
			{
				lst = new List<Pair>();
				lst.Add(pair);
				Dbg.DEBUG_MSG("Event::register: event(" + eventname + ")!");
				events.Add(eventname, lst);
				Monitor.Exit(events);
				return true;
			}
			
			Dbg.DEBUG_MSG("Event::register: event(" + eventname + ")!");
			lst.Add(pair);
			Monitor.Exit(events);
			return true;
		}

		public static bool deregisterOut(string eventname, object obj, string funcname)
		{
			return deregister(events_out, eventname, obj, funcname);
		}

		public static bool deregisterIn(string eventname, object obj, string funcname)
		{
			return deregister(events_in, eventname, obj, funcname);
		}
		
		private static bool deregister(Dictionary<string, List<Pair>> events, string eventname, object obj, string funcname)
		{
			Monitor.Enter(events_out);
			List<Pair> lst = null;
			
			if(!events_out.TryGetValue(eventname, out lst))
			{
				Monitor.Exit(events_out);
				return false;
			}
			
			for(int i=0; i<lst.Count; i++)
			{
				if(obj == lst[i].obj && lst[i].funcname == funcname)
				{
					Dbg.DEBUG_MSG("Event::deregister: event(" + eventname + ":" + funcname + ")!");
					lst.RemoveAt(i);
					Monitor.Exit(events_out);
					return true;
				}
			}
			
			Monitor.Exit(events_out);
			return false;
		}

		public static bool deregisterOut(object obj)
		{
			return deregister(events_out, obj);
		}

		public static bool deregisterIn(object obj)
		{
			return deregister(events_in, obj);
		}
		
		private static bool deregister(Dictionary<string, List<Pair>> events, object obj)
		{
			Monitor.Enter(events);
			
			foreach(KeyValuePair<string, List<Pair>> e in events)
			{
				List<Pair> lst = e.Value;
__RESTART_REMOVE:
				for(int i=0; i<lst.Count; i++)
				{
					if(obj == lst[i].obj)
					{
						Dbg.DEBUG_MSG("Event::deregister: event(" + e.Key + ":" + lst[i].funcname + ")!");
						lst.RemoveAt(i);
						goto __RESTART_REMOVE;
					}
				}
			}
			
			Monitor.Exit(events);
			return true;
		}

		public static void fireOut(string eventname, object[] args)
		{
			fire(events_out, firedEvents_out, eventname, args);
		}

		public static void fireIn(string eventname, object[] args)
		{
			fire(events_in, firedEvents_in, eventname, args);
		}
		
		private static void fire(Dictionary<string, List<Pair>> events, List<EventObj> firedEvents, string eventname, object[] args)
		{
			Monitor.Enter(events);
			List<Pair> lst = null;
			
			if(!events.TryGetValue(eventname, out lst))
			{
				Dbg.ERROR_MSG("Event::fire: event(" + eventname + ") not found!");
				Monitor.Exit(events);
				return;
			}
			
			for(int i=0; i<lst.Count; i++)
			{
				EventObj eobj = new EventObj();
				eobj.info = lst[i];
				eobj.args = args;
				firedEvents.Add(eobj);
			}
			
			Monitor.Exit(events);
		}
		
		public static void processEventsMainThread()
		{
			Monitor.Enter(events_out);

			if(firedEvents_out.Count > 0)
			{
				foreach(EventObj evt in firedEvents_out)
				{
					doingEvents_out.Add(evt);
				}

				firedEvents_out.Clear();
			}

			Monitor.Exit(events_out);
			
			for(int i=0; i<doingEvents_out.Count; i++)
			{
				EventObj eobj = doingEvents_out[i];
				
				//Debug.Log("processEventsMainThread:" + eobj.info.funcname + "(" + eobj.info + ")");
				//foreach(object v in eobj.args)
				//{
				//	Debug.Log("processEventsMainThread:args=" + v);
				//}
				
				eobj.info.method.Invoke(eobj.info.obj, eobj.args);
			}
			
			doingEvents_out.Clear();
		}
		
		public static void processEventsKBEThread()
		{
			Monitor.Enter(events_in);

			if(firedEvents_in.Count > 0)
			{
				foreach(EventObj evt in firedEvents_in)
				{
					doingEvents_in.Add(evt);
				}

				firedEvents_in.Clear();
			}

			Monitor.Exit(events_in);
			
			for(int i=0; i<doingEvents_in.Count; i++)
			{
				EventObj eobj = doingEvents_in[i];
				
				//Debug.Log("processEventsMainThread:" + eobj.info.funcname + "(" + eobj.info + ")");
				//foreach(object v in eobj.args)
				//{
				//	Debug.Log("processEventsMainThread:args=" + v);
				//}
				
				eobj.info.method.Invoke(eobj.info.obj, eobj.args);
			}
			
			doingEvents_in.Clear();
		}
	
    }
} 
