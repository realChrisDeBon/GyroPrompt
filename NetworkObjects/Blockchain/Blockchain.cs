using System;
using System.Collections.Generic;
using System.Text;

namespace BlockchainSpace
{
    public class Blockchain
    {
        public IList<Block> Chain { set; get; }
        public int Difficulty { set; get; } = 3;
        public IList<Transaction> PendingTransaction = new List<Transaction>();
        public string BlockchainName { get; set; }

        public Blockchain()
        {
            InitializeChain();
            AddGenesisBlock();
        }
         
        public void InitializeChain()
        {
            Chain = new List<Block>();
        }

        public Block CreateGenesisBlock()
        {
            return new Block(DateTime.Now, null, "Genesis Block");
        }

        public void AddGenesisBlock()
        {
            Chain.Add(CreateGenesisBlock());
        }

        public Block GetLatestBlock()
        {
            return Chain[Chain.Count - 1];
        }

        public void AddBlock(Block block)
        {
            Block latestBlock = GetLatestBlock();
            block.Index = latestBlock.Index + 1;
            block.PreviousHash = latestBlock.Hash;
            block.Mine(this.Difficulty);
            Chain.Add(block);
        }

        public void CreateTransaction(Transaction transaction)
        {
            PendingTransaction.Add(transaction);  
        }

        public void ProcessPendingTransactions(string minerAddress)
        {
            Block block = new Block(DateTime.Now, GetLatestBlock().Hash, "Processing Block");
            AddBlock(block);
            PendingTransaction = new List<Transaction>();
            CreateTransaction(new Transaction(null, minerAddress, 5));
        }

        public bool IsValid()
        {
            for (int i = 1; i < Chain.Count; i++)
            {
                Block currentBlock = Chain[i];
                Block previousBlock = Chain[i];

                if (currentBlock.Hash != currentBlock.CalculateHash())
                {
                    return false;
                }

                if (currentBlock.PreviousHash != previousBlock.Hash)
                {
                    return false;
                }
            }
            return true;
        }

        public void NameChain(string name)
        {
            BlockchainName = name;
        }
    }
}
