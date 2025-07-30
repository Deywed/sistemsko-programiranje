class Book {
  final String title;
  final String author;
  final int rank;

  Book({required this.title, required this.author, required this.rank});

  factory Book.fromJson(Map<String, dynamic> json) {
    return Book(
      title: json['title'] as String,
      author: json['author'] as String,
      rank: json['rank'] as int,
    );
  }
}
