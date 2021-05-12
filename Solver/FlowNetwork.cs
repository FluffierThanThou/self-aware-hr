// Copyright Karel Kroeze, 2021-2021.
// SelfAwareHR/SelfAwareHR/FlowNetwork.cs

using System;
using System.Collections.Generic;
using System.Text;

namespace SelfAwareHR.Solver
{
    public class FlowNetwork
    {
        private readonly List<List<FlowEdge>> _adj = new List<List<FlowEdge>>();

        public FlowNetwork(int nodeCount)
        {
            V = nodeCount;
            for (var i = 0; i < nodeCount; i++)
            {
                _adj.Add(new List<FlowEdge>());
            }
        }

        public int E { get; private set; }
        public int V { get; private set; }

        public void AddEdge(FlowEdge e)
        {
            var v = e.From;
            var w = e.To;
            ValidateVertex(v);
            ValidateVertex(w);
            _adj[v].Add(e);
            _adj[w].Add(e);
            E++;
        }

        public void AddEdge(int from, int to, int capacity = 1, int flow = 0)
        {
            AddEdge(new FlowEdge(from, to, capacity, flow));
        }

        public List<FlowEdge> Adj(int v)
        {
            ValidateVertex(v);
            return _adj[v];
        }

        public List<FlowEdge> Edges()
        {
            var list = new List<FlowEdge>();
            for (var i = 0; i < V; i++)
            {
                foreach (var e in _adj[i])
                {
                    if (e.To != i)
                    {
                        list.Add(e);
                    }
                }
            }

            return list;
        }

        public IEnumerable<FlowEdge> In(int v)
        {
            ValidateVertex(v);
            return _adj[v].Where(e => e.To == v);
        }

        public IEnumerable<FlowEdge> Out(int v)
        {
            ValidateVertex(v);
            return _adj[v].Where(e => e.From == v);
        }

        public new string ToString()
        {
            var s = new StringBuilder();
            for (var v = 0; v < V; v++)
            {
                s.Append($"{v}: ");
                foreach (var e in _adj[v])
                {
                    if (e.To != v)
                    {
                        s.Append(e.ToString() + " ");
                    }
                }

                s.AppendLine();
            }

            return s.ToString();
        }

        private void ValidateVertex(int v)
        {
            if (v < 0 || v >= V)
            {
                throw new IndexOutOfRangeException();
            }
        }
    }
}