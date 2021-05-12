// Copyright Karel Kroeze, 2021-2021.
// SelfAwareHR/SelfAwareHR/FlowEdge.cs

using System;

namespace SelfAwareHR.Solver
{
    public class FlowEdge
    {
        public FlowEdge(int from, int to, double capacity, double flow)
        {
            if (from < 0)
            {
                throw new IndexOutOfRangeException();
            }

            if (to < 0)
            {
                throw new IndexOutOfRangeException();
            }

            if (!(capacity >= 0.0))
            {
                throw new ArgumentException();
            }

            if (!(flow <= capacity))
            {
                throw new ArgumentException();
            }

            if (!(flow >= 0.0))
            {
                throw new ArgumentException();
            }

            From     = from;
            To       = to;
            Capacity = capacity;
            Flow     = flow;
        }

        public FlowEdge(int from, int to, double capacity) : this(from, to, capacity, 0.0)
        {
        }

        public FlowEdge(FlowEdge e) : this(e.From, e.To, e.Capacity, e.Flow)
        {
        }

        public double Capacity { get; }
        public double Flow     { get; private set; }
        public int    From     { get; }
        public int    To       { get; }

        public void AddResidualFlowTo(int vertex, double delta)
        {
            if (vertex == From)
            {
                Flow -= delta;
            }
            else if (vertex == To)
            {
                Flow += delta;
            }
            else
            {
                throw new ArgumentException();
            }

            if (double.IsNaN(delta))
            {
                throw new ArgumentException();
            }

            if (!(Flow >= 0.0))
            {
                throw new ArgumentException();
            }

            if (!(Flow <= Capacity))
            {
                throw new ArgumentException();
            }
        }

        public int Other(int vertex)
        {
            if (vertex == From)
            {
                return To;
            }

            if (vertex == To)
            {
                return From;
            }

            throw new ArgumentException();
        }

        public double ResidualCapacityTo(int vertex)
        {
            if (vertex == From)
            {
                return Flow;
            }

            if (vertex == To)
            {
                return Capacity - Flow;
            }

            throw new ArgumentException();
        }

        public override string ToString()
        {
            return From + "->" + To + " " + Flow + "/" + Capacity;
        }
    }
}