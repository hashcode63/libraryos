-- LibraryOS Database Schema for Neon PostgreSQL

-- Books Table
CREATE TABLE IF NOT EXISTS books (
    id SERIAL PRIMARY KEY,
    title VARCHAR(255) NOT NULL,
    author VARCHAR(255) NOT NULL,
    isbn VARCHAR(20) UNIQUE NOT NULL,
    category VARCHAR(100),
    book_link TEXT,
    is_available BOOLEAN DEFAULT true,
    uploaded_by INTEGER,
    upload_status VARCHAR(20) DEFAULT 'approved', -- 'pending', 'approved', 'rejected'
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Patrons Table
CREATE TABLE IF NOT EXISTS patrons (
    id SERIAL PRIMARY KEY,
    name VARCHAR(255) NOT NULL,
    email VARCHAR(255) UNIQUE NOT NULL,
    phone VARCHAR(20),
    registration_date TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    max_books INTEGER DEFAULT 3,
    is_active BOOLEAN DEFAULT true
);

-- Loans Table
CREATE TABLE IF NOT EXISTS loans (
    id SERIAL PRIMARY KEY,
    book_id INTEGER NOT NULL REFERENCES books(id),
    patron_id INTEGER NOT NULL REFERENCES patrons(id),
    borrowed_date DATE NOT NULL,
    due_date DATE NOT NULL,
    return_date DATE,
    is_returned BOOLEAN DEFAULT false,
    borrow_fee DECIMAL(10, 2) DEFAULT 100.00,
    overdue_fine DECIMAL(10, 2) DEFAULT 0.00,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Indexes for better performance
CREATE INDEX idx_books_isbn ON books(isbn);
CREATE INDEX idx_books_available ON books(is_available);
CREATE INDEX idx_patrons_email ON patrons(email);
CREATE INDEX idx_loans_patron ON loans(patron_id);
CREATE INDEX idx_loans_book ON loans(book_id);
CREATE INDEX idx_loans_returned ON loans(is_returned);

-- Sample Data (Optional - for testing)
INSERT INTO books (title, author, isbn, category, book_link) VALUES
('Clean Code', 'Robert C. Martin', '9780132350884', 'Programming', 'https://example.com/books/clean-code.pdf'),
('Design Patterns', 'Gang of Four', '9780201633612', 'Software Engineering', 'https://example.com/books/design-patterns.pdf'),
('The Pragmatic Programmer', 'Andrew Hunt', '9780135957059', 'Programming', 'https://example.com/books/pragmatic.pdf');

INSERT INTO patrons (name, email, phone) VALUES
('John Doe', 'john@example.com', '08012345678'),
('Jane Smith', 'jane@example.com', '08087654321');
