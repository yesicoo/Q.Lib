using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Q.Lib.QSocket
{
    class ServerClientPool
    {
        Stack<ServerClient> m_pool;


        public ServerClientPool(int capacity)
        {
            m_pool = new Stack<ServerClient>(capacity);
        }

        public void Push(ServerClient item)
        {
            if (item == null) { throw new ArgumentNullException("Items added to a SocketAsyncEventArgsPool cannot be null"); }
            lock (m_pool)
            {
                m_pool.Push(item);
            }
        }

        // Removes a SocketAsyncEventArgs instance from the pool  
        // and returns the object removed from the pool  
        public ServerClient Pop()
        {
            lock (m_pool)
            {
                return m_pool.Pop();
            }
        }

        // The number of SocketAsyncEventArgs instances in the pool  
        public int Count
        {
            get { return m_pool.Count; }
        }

        public void Clear()
        {
            m_pool.Clear();
        }
    }
}
