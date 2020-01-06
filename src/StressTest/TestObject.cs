using System.Threading;

namespace SimpleStressTest
{
    public class TestObject
    {
        public void Init()
        {
        }

        /// <summary>
        /// 写数据
        /// </summary>
        /// <param name="id"></param>
        public void Write(int id)
        {
            Thread.Sleep(id);
        }

        /// <summary>
        /// 读数据
        /// </summary>
        /// <param name="id"></param>
        public void Read(int id)
        {
            Thread.Sleep(id);
        }
    }
}