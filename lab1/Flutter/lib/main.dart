import 'package:flutter/material.dart';
import 'package:lab1/repositories/book_repo.dart';
import 'models/book.dart';

void main() {
  WidgetsFlutterBinding.ensureInitialized();
  runApp(MainApp());
}

class MainApp extends StatefulWidget {
  const MainApp({super.key});

  @override
  State<MainApp> createState() => _MainAppState();
}

class _MainAppState extends State<MainApp> {
  final controller = TextEditingController();
  final repo = BookRepo();

  List<Book> books = [];
  String error = '';

  void _search() async {
    final category = controller.text.trim();
    if (category.isEmpty) return;

    print('Fetching books for category: $category');

    try {
      final result = await repo.fetchBooks(category);
      print('Received ${result.length} books');
      setState(() {
        books = result;
      });
    } catch (e) {
      print('Error: $e');

      setState(() {
        books = [];
        error = "Category not found";
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      debugShowCheckedModeBanner: false,
      home: Scaffold(
        appBar: AppBar(title: Text("NYT Best Sellers")),
        body: Padding(
          padding: EdgeInsets.symmetric(vertical: 20, horizontal: 16),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                'Enter a NYT list category (audio-fiction, '
                'audio-nonfiction, '
                'hardcover-fiction, '
                'hardcover-nonfiction, '
                'paperback-fiction, '
                'paperback-nonfiction):',
              ),
              TextField(
                controller: controller,
                decoration: InputDecoration(hintText: "Enter category..."),
              ),
              SizedBox(height: 10),
              ElevatedButton(
                onPressed: () {
                  setState(() {
                    books = [];
                  });
                  _search();
                },
                child: Text("Search"),
              ),
              Expanded(
                child: ListView.builder(
                  itemCount: books.length,
                  itemBuilder: (_, index) {
                    final book = books[index];
                    return ListTile(
                      title: Text("Rank: ${book.rank}"),
                      subtitle: Text(book.title),

                      trailing: Text(book.author),
                      isThreeLine: true,
                    );
                  },
                ),
              ),
              if (error != '')
                Text(error, style: TextStyle(color: Colors.red, fontSize: 30)),
            ],
          ),
        ),
      ),
    );
  }
}
