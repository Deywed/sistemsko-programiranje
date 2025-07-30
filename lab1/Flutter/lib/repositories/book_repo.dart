import 'dart:convert';

import 'package:http/http.dart' as http;
import 'package:lab1/models/book.dart';

class BookRepo {
  final String baseUrl = "http://localhost:8080";

  Future<List<Book>> fetchBooks(String list) async {
    final url = Uri.parse('$baseUrl/?category=$list');

    final response = await http.get(url);

    if (response.statusCode == 200) {
      final decoded = jsonDecode(response.body);
      final List<dynamic> data = decoded['results']['books'];
      return data.map((bookJson) => Book.fromJson(bookJson)).toList();
    } else {
      throw Exception('Failed to load books: ${response.statusCode}');
    }
  }
}
