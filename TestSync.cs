using System;
using PharmaBilling.Source.Data;

class Program {
    static void Main() {
        try {
            Console.WriteLine("Testing Sync...");
            CloudSyncService.SyncRecentDataAsync();
            Console.WriteLine("Sync method called. Waiting for task...");
            System.Threading.Thread.Sleep(5000);
            Console.WriteLine("Done.");
        } catch(Exception e) {
            Console.WriteLine(e);
        }
    }
}
