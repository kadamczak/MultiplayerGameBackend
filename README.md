# Barvon - Game Backend

The backend API for Barvon, a 2D MMO dragon game. Provides secure authentication, item trading, and player management services.

## Features

- **Authentication & Authorization**: 
  - JWT-based access tokens with refresh token rotation
  - Email confirmation on registration
  - Support for authentication from both the web browser and the game
  - Password reset and change, account deletion

- **Item Trading System**:
  - Player-to-player item offers
  - Merchant NPC item offers
  - Keyword search, pagination and sorting of offers and items

- **Inventory Management**:
  - User item tracking
  - Coming soon: Item equipping system (head and body slots)

- **Player Customization**:
  - Dragon appearance customization (colors, body part types)
  - Profile picture upload with compression (max 2MB)

- **Background Services**:
  - Automatic cleanup of expired refresh tokens
  - Removal of unconfirmed accounts after 24 hours

## Tech Stack

- **Framework**: ASP.NET Core Web API
- **Database**: PostgreSQL with Entity Framework Core
- **Authentication**: ASP.NET Core Identity with JWT tokens
- **Email**: SMTP email service
- **Image Processing**: Image compression for profile pictures
- **Background Jobs**: Hosted background services
- **Logging**: Serilog with console and file output
- **Validation**: DataAnnotations and FluentValidation

## Architecture

Clean Architecture with the following layers:
- **API**: Controllers, middleware, and HTTP concerns
- **Application**: Business logic, DTOs, and service interfaces
- **Domain**: Entities, constants, and domain exceptions
- **Infrastructure**: Database access, external services, and background jobs


## API Documentation

The API follows RESTful conventions and includes the following main endpoints:

- `/v1/identity` - Authentication (register, login, refresh, logout, password management)
- `/v1/users` - User management and customization
- `/v1/items` - Management of general item types
- `/v1/users/me/items` - User inventory (concrete item instances)
- `/v1/users/offers` - Player-to-player trading
- `/v1/merchants/offers` - Merchant NPC item offers


