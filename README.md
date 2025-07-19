
# 🎧 LectureBuddy

LectureBuddy es una aplicación desarrollada en C#/.NET para el procesamiento y manipulación de archivos de audio, orientada a facilitar tareas educativas y de laboratorio.

## 🚀 Características principales

- Procesamiento y conversión de archivos de audio (WAV, MP3).
- Integración con librerías NAudio y NAudio.Lame para manejo avanzado de audio.
- Manipulación de datos en formato JSON.
- Comunicación HTTP para integración con servicios externos.
- Estructura modular y fácil de extender.

## 📦 Dependencias del proyecto

Ejecuta estos comandos en la raíz del proyecto para instalar las librerías necesarias:

```bash
# Procesamiento de audio
dotnet add package NAudio --version 2.2.1
dotnet add package NAudio.Lame --version 2.1.0

# Manipulación de JSON y HTTP
dotnet add package Newtonsoft.Json --version 13.0.3
dotnet add package System.Net.Http --version 4.3.4
```

## 🐍 Entorno virtual y dependencias Python

El proyecto utiliza scripts o módulos en Python, sigue estos pasos para crear y activar el entorno virtual e instalar dependencias:

```bash
# Crear el entorno virtual
python -m venv venv

# Activar el entorno virtual (Windows)
venv\Scripts\activate

# Instalar dependencias
pip install -r requirements.txt
```

## 🗂 Estructura del proyecto

- `Program.cs`: Archivo principal de la aplicación.
- `README.md`: Este documento.
- `.gitignore`: Configuración para ignorar archivos y carpetas innecesarias.
- Otros archivos y carpetas para respaldo, documentación y pruebas.

## ⚡ Uso rápido

1. Clona el repositorio.
2. Instala las dependencias con los comandos anteriores.
3. Ejecuta el proyecto con Visual Studio o usando la CLI de .NET.
