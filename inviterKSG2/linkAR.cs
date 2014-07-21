using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace admnNotify
{
    class linkAR
    {

        private Cont first=null, last=null,current=null,temp=null;
        private int length = 0;
        private int width;
        public linkAR(int width)
        {
            this.width = width;
        }

        public void AddEnd()
        {
            if (first == null)
            {
                first = new Cont(width);
                last = first;
                first.next = last;
                first.prev = last;
                current=first;
            }
            else
            {
                temp = new Cont(width);
                last.next = temp;
                temp.prev= last;
                last = temp;
                last.next = first;
                first.prev = last;
                current = last;
            }
            length++;

        }
        public void deleteFirst()
        {
            if (first == last)
            {
                this.deleteAll();
            }
            else
            {
                last.next = first.next;
                first.next.prev = last;
                if (current == first)
                {
                    current = first.next;
                }
                first = first.next;
            }
            length--;
        }
        public void deleteAll()
        {
            last.next = null;
            last.prev = null;
            first.prev = null;
            first.next = null;
            last = null;
            first = null;
            current = null;
            length = 0;
        }
        public String[] getCurrent()
        {
            if (current != null)
                return current.AR;
            else
                return null;
        }
        public void next()
        {
            current = current.next;
        }
        public void prev()
        {
            current = current.prev;
        }
        public bool isLast()
        {
            return (current == last);
            
        }
        public bool isFirst()
        {
            return (current == first);

        }
        public void setFirst()
        {
            current = first;
        }
        public void setLast()
        {
            current = last;
        }
        public int getLength()
        {
            return length;
        }
    }
    class Cont
    {
        public String[] AR;
        public Cont next, prev;
        public Cont(int length)
        {
            AR = new String[length];

        }

    }



}
