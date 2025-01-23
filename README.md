# EcommerceProject
<p>This is an E-Commerce platform built using <strong>ASP.NET Core Web API</strong> for the backend and <strong>Angular</strong> for the frontend. The project follows a Model-Controller-Database approach and uses <strong>SQL Server</strong> as the database for storing and managing data. It also implements <strong>Authentication</strong> and <strong>Authorization</strong> to ensure secure access for users and admins.</p>

<hr>

<h2>Features</h2>
<ul>
    <li><strong>Product Management:</strong> Add and manage products.</li>
    <li><strong>Shop Management:</strong> Create, edit, and delete shops.</li>
    <li><strong>User Management:</strong> Add, update, delete users, and manage their roles.</li>
    <li><strong>Authentication & Authorization:</strong> Secure login and access management with roles (Admin, User).</li>
    <li><strong>CRUD Operations:</strong> Full Create, Read, Update, Delete functionality for products, shops, and users.</li>
</ul>

<hr>

<h2>Backend</h2>

<h3>Technologies Used</h3>
<ul>
    <li><strong>ASP.NET Core Web API:</strong> Backend API to handle business logic and interact with the database.</li>
    <li><strong>SQL Server:</strong> Database to store products, shops, and user data.</li>
    <li><strong>JWT Authentication:</strong> Secure authentication for users and roles.</li>
</ul>

<h3>API Endpoints</h3>

<h4>Product Endpoints</h4>
<ul>
    <li><strong>POST /api/Product/AddProduct:</strong> Add a new product.</li>
    <li><strong>GET /api/Product/GetProduct/{productId}:</strong> Get product details by ID.</li>
</ul>

<h4>Shop Endpoints</h4>
<ul>
    <li><strong>POST /api/Shop/CreateShop:</strong> Create a new shop.</li>
    <li><strong>PUT /api/Shop/EditShop:</strong> Edit an existing shop.</li>
    <li><strong>PUT /api/Shop/SoftDeleteShop/{shopId}:</strong> Soft delete a shop.</li>
    <li><strong>GET /api/Shop/GetShop/{shopId}:</strong> Get shop details by ID.</li>
</ul>

<h4>User Endpoints</h4>
<ul>
    <li><strong>GET /api/User:</strong> Get a list of all users.</li>
    <li><strong>GET /api/User/{id}:</strong> Get user details by ID.</li>
    <li><strong>POST /api/User/AddUser:</strong> Add a new user.</li>
    <li><strong>PUT /api/User/UpdateUser/{userId}:</strong> Update user information.</li>
    <li><strong>DELETE /api/User/delete/{userId}:</strong> Delete a user.</li>
</ul>

<h3>Authentication & Authorization</h3>
<ul>
    <li><strong>JWT Tokens</strong> are used for authentication and authorization.</li>
    <li><strong>Roles:</strong>
        <ul>
            <li><strong>Admin:</strong> Can perform all operations (CRUD on products, shops, users).</li>
            <li><strong>User:</strong> Can perform operations based on user permissions.</li>
        </ul>
    </li>
</ul>

<hr>

<h2>Frontend</h2>

<h3>Technologies Used</h3>
<ul>
    <li><strong>Angular:</strong> Frontend for managing the UI and connecting with the backend via API calls.</li>
    <li><strong>Bootstrap:</strong> Used for responsive design and UI components.</li>
</ul>

<h3>Features</h3>
<ul>
    <li>User-friendly interface to manage products, shops, and users.</li>
    <li>Authentication forms for user login.</li>
    <li>Admin dashboard for managing all resources.</li>
</ul>

<h3>How to Run the Frontend</h3>
<ol>
    <li>Clone the repository:
        <pre><code>git clone &lt;repo-link&gt;</code></pre>
    </li>
    <li>Navigate to the project directory:
        <pre><code>cd e-commerce-frontend</code></pre>
    </li>
    <li>Install dependencies:
        <pre><code>npm install</code></pre>
    </li>
    <li>Run the Angular application:
        <pre><code>ng serve</code></pre>
    </li>
    <li>Open the application in your browser:
        <pre><code>http://localhost:4200</code></pre>
    </li>
</ol>

<h3>How to Run the Backend</h3>
<ol>
    <li>Clone the repository:
        <pre><code>git clone &lt;repo-link&gt;</code></pre>
    </li>
    <li>Navigate to the backend project directory:
        <pre><code>cd e-commerce-backend</code></pre>
    </li>
    <li>Build the project:
        <pre><code>dotnet build</code></pre>
    </li>
    <li>Run the project:
        <pre><code>dotnet run</code></pre>
    </li>
    <li>Access the backend API at:
        <pre><code>http://localhost:5000/api</code></pre>
    </li>
</ol>

<hr>

<h2>Database Setup</h2>
<ol>
    <li>Set up <strong>SQL Server</strong> locally or use a cloud-based instance.</li>
    <li>Create a new database named <strong>ECommerceDb</strong>.</li>
    <li>Apply migrations to set up the necessary tables and relationships:
        <pre><code>dotnet ef database update</code></pre>
    </li>
</ol>

<hr>

<h2>License</h2>
<p>This project is licensed under the <strong>MIT License</strong> - see the <a href="LICENSE">LICENSE</a> file for details.</p>
