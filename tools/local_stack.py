"""Local Windows launcher. Owns only child processes it starts; Ctrl+C stops the stack.
Portable PostgreSQL and Ollama are kept outside the repository (DOCUMENTFLOW_RUNTIME).
No credentials, original files, or database contents are written to the repository.
"""
from pathlib import Path
import argparse
import json
import os
import secrets
import shutil
import socket
import subprocess
import time
import urllib.request
import webbrowser
from contextlib import contextmanager

SOURCE = Path(__file__).resolve().parents[1]


def resolve_runtime(source=SOURCE):
    configured = os.environ.get('DOCUMENTFLOW_RUNTIME')
    if configured:
        runtime = Path(configured).expanduser().resolve()
        if not runtime.is_dir():
            raise RuntimeError(f'DOCUMENTFLOW_RUNTIME does not exist: {runtime}. No runtime or secrets were created.')
        return runtime
    # Both a flat Git worktree and the original deeply nested package can share an existing runtime.
    for ancestor in (source, *source.parents):
        candidate = ancestor / 'DocumentFlow-runtime'
        if candidate.is_dir():
            return candidate.resolve()
    raise RuntimeError('Existing DocumentFlow-runtime not found beside source ancestors. Set DOCUMENTFLOW_RUNTIME; no runtime or secrets were created.')


RUNTIME = resolve_runtime()
API_URL = 'http://127.0.0.1:5214'
WEB_URL = 'http://localhost:5173'


def configure_ocr_environment(env, runtime=RUNTIME):
    """Only child environment is changed. Never install OCR or modify the machine PATH."""
    configured = env.get('Tesseract__ExecutablePath', 'tesseract')
    executable = shutil.which(configured, path=env.get('PATH'))
    # A custom path/name is intentional: do not mask its errors with another installation.
    if executable is None and configured.lower() in {'tesseract', 'tesseract.exe'}:
        candidates = [runtime / 'tesseract/tesseract.exe', runtime / 'Tesseract-OCR/tesseract.exe',
                      runtime / 'tesseract/bin/tesseract.exe']
        for variable in ('ProgramW6432', 'ProgramFiles', 'ProgramFiles(x86)', 'LOCALAPPDATA'):
            root = env.get(variable)
            if root:
                candidates.append(Path(root) / 'Tesseract-OCR/tesseract.exe')
                if variable == 'LOCALAPPDATA':
                    candidates.append(Path(root) / 'Programs/Tesseract-OCR/tesseract.exe')
        executable = next((str(path.resolve()) for path in candidates if path.is_file()), None)
    if executable is None and Path(configured).is_file():
        executable = str(Path(configured).resolve())
    env.setdefault('Tesseract__Languages', 'rus+eng')
    env['DOCUMENTFLOW_RUNTIME'] = str(runtime)
    if executable is None:
        print('[ocr_executable_unavailable] Tesseract not found. Install it with rus/eng.traineddata; set Tesseract__ExecutablePath. OCR documents will fail; DOCX/text PDFs remain usable.', flush=True)
        return 'Unavailable'
    env['Tesseract__ExecutablePath'] = executable
    data = env.get('Tesseract__TessdataPath')
    if not data and not env.get('TESSDATA_PREFIX'):
        candidates = (Path(executable).parent / 'tessdata', Path(executable).parent.parent / 'share/tessdata')
        data = next((str(path) for path in candidates if path.is_dir()), None)
        if data:
            env['Tesseract__TessdataPath'] = data
    arguments = [executable]
    if data:
        arguments += ['--tessdata-dir', data]
    arguments.append('--list-langs')
    try:
        result = subprocess.run(arguments, env=env, capture_output=True, timeout=5, encoding='utf-8', errors='replace')
    except subprocess.TimeoutExpired:
        print('[ocr_timeout] Tesseract language preflight exceeded 5 seconds.', flush=True)
        return 'Timeout'
    except OSError:
        print('[ocr_executable_unavailable] Tesseract could not start. Check the configured executable and its native dependencies.', flush=True)
        return 'Unavailable'
    if result.returncode:
        language_error = any(text in result.stderr.lower() for text in
                             ('failed loading language', 'error opening data file', "couldn't load any languages"))
        code = 'ocr_missing_languages' if language_error else 'ocr_process_failed'
        print(f'[{code}] Tesseract language preflight exited {result.returncode}. Check Tesseract__TessdataPath and installation.', flush=True)
        return 'MissingLanguages' if language_error else 'Failed'
    available = {line.strip() for line in (result.stdout + '\n' + result.stderr).splitlines()
                 if line.strip() and len(line.strip()) <= 80 and
                 ('/' not in line.strip() or (line.strip().startswith('script/') and line.strip().count('/') == 1 and not line.strip().endswith('/'))) and
                 all(character.isascii() and (character.isalnum() or character in '_/') for character in line.strip())}
    required = {language.strip() for language in env['Tesseract__Languages'].split('+') if language.strip()}
    missing = required - available
    if missing or not required:
        print('[ocr_missing_languages] Missing configured languages: ' + ', '.join(sorted(missing)) +
              '. Install matching .traineddata and check Tesseract__TessdataPath.', flush=True)
        return 'MissingLanguages'
    print('OCR preflight: Healthy; languages=' + '+'.join(sorted(required)), flush=True)
    return 'Healthy'



def run(args, cwd=SOURCE, env=None):
    # pg_ctl on Windows can leave inherited pipe handles open in its server child.
    # File-backed capture waits for the command itself, not for descendant pipe EOF.
    import tempfile
    with tempfile.TemporaryFile(dir=RUNTIME) as output:
        result = subprocess.run([str(arg) for arg in args], cwd=cwd, env=env, stdout=output, stderr=subprocess.STDOUT, timeout=180)
        output.seek(0)
        text = output.read().decode('utf-8', errors='replace')
    if result.returncode:
        raise RuntimeError(f'Command failed: {Path(str(args[0])).name}\n{text}')
    return text


def wait_http(url, process, seconds=60):
    deadline = time.monotonic() + seconds
    while time.monotonic() < deadline:
        if process.poll() is not None:
            raise RuntimeError(f'{url} process exited. See runtime logs.')
        try:
            with urllib.request.urlopen(url, timeout=2) as response:
                if response.status == 200:
                    return
        except (OSError, TimeoutError):
            time.sleep(.2)
    raise RuntimeError(f'{url} did not become ready. See runtime logs.')


@contextmanager
def stack(database='documentflow', build=True):
    import re
    if not re.fullmatch(r'[a-z][a-z0-9_]{0,62}', database):
        raise ValueError('Invalid local database identifier')
    for port in [5214, 5173, 15439, 11445]:
        with socket.socket() as connection:
            if connection.connect_ex(('127.0.0.1', port)) == 0:
                raise RuntimeError(f'Port {port} is in use. Existing processes were NOT stopped.')
    for relative in ['pgsql/bin/pg_ctl.exe', 'ollama/ollama.exe', 'pgdata/PG_VERSION']:
        if not (RUNTIME / relative).is_file():
            raise RuntimeError(f'Missing {relative}. See README-LOCAL.md; DOCUMENTFLOW_RUNTIME={RUNTIME}')
    private_file = RUNTIME / 'local-secrets.json'
    if not private_file.exists():
        raise RuntimeError('Missing local-secrets.json. Configure the runtime as described in README-LOCAL.md.')
    private = json.loads(private_file.read_text(encoding='utf-8'))
    env = os.environ.copy()
    env.update({
        'PGPASSWORD': private['databasePassword'], 'PGCLIENTENCODING': 'UTF8',
        'ConnectionStrings__Postgres': f"Host=127.0.0.1;Port=15439;Database={database};Username=documentflow;Password={private['databasePassword']}",
        'DocumentStorage__RootPath': str(RUNTIME / 'documents' / database),
        'Auth__Jwt__Issuer': 'DocumentFlow.Local', 'Auth__Jwt__Audience': 'DocumentFlow.Local.Web',
        'Auth__Jwt__SigningKey': private['jwtKey'], 'Auth__Jwt__LifetimeMinutes': '240',
        'Auth__BootstrapAdmin__UserName': 'admin', 'Auth__BootstrapAdmin__DisplayName': 'Администратор',
        'Auth__BootstrapAdmin__Password': private['adminPassword'],
        'Cors__AllowedOrigins__0': WEB_URL, 'Cors__AllowedOrigins__1': 'http://127.0.0.1:5173',
        'ASPNETCORE_URLS': API_URL, 'ASPNETCORE_ENVIRONMENT': 'Development', 'DOTNET_ENVIRONMENT': 'Development',
        'Database__ApplyMigrations': 'true', 'SemanticSearch__Enabled': 'true',
        'SemanticSearch__BaseUrl': 'http://127.0.0.1:11445', 'SemanticSearch__Model': 'bge-m3',
        'SemanticSearch__IntervalSeconds': '5', 'SemanticSearch__TimeoutSeconds': '120',
        'AiReview__Enabled': 'true', 'Ollama__BaseUrl': 'http://127.0.0.1:11445',
        'Ollama__Model': 'qwen2.5:0.5b-instruct', 'Ollama__TimeoutSeconds': '120',
        'OLLAMA_HOST': '127.0.0.1:11445', 'OLLAMA_MODELS': str(RUNTIME / 'models'),
        'OLLAMA_NO_CLOUD': '1', 'OLLAMA_KEEP_ALIVE': '2m',
        'VITE_API_BASE_URL': API_URL,
    })
    configure_ocr_environment(env)
    children = []
    logs = []
    postgres_started = False
    def start(name, args, cwd=SOURCE):
        log = (RUNTIME / f'{name}.log').open('w', encoding='utf-8'); logs.append(log)
        child = subprocess.Popen([str(arg) for arg in args], cwd=cwd, env=env, stdout=log, stderr=subprocess.STDOUT)
        children.append(child)
        return child
    try:
        if build:
            print('Building API, Worker and frontend...', flush=True)
            for project in ['Api', 'Worker']:
                run(['dotnet', 'build', f'src/DocumentFlow.{project}/DocumentFlow.{project}.csproj', '-v', 'quiet'], env=env)
            npm = 'npm.cmd' if os.name == 'nt' else 'npm'
            run([npm, 'ci'], cwd=SOURCE / 'frontend/documentflow-web', env=env)
            run([npm, 'run', 'build'], cwd=SOURCE / 'frontend/documentflow-web', env=env)
        run([RUNTIME / 'pgsql/bin/pg_ctl.exe', '-D', RUNTIME / 'pgdata', '-l', RUNTIME / 'postgres.log', '-o', '-p 15439 -h 127.0.0.1', '-w', 'start'], env=env)
        postgres_started = True
        exists = run([RUNTIME / 'pgsql/bin/psql.exe', '-h', '127.0.0.1', '-p', '15439', '-U', 'documentflow', '-d', 'postgres', '-Atc', f"SELECT 1 FROM pg_database WHERE datname='{database}'"], env=env)
        if exists.strip() != '1':
            run([RUNTIME / 'pgsql/bin/createdb.exe', '-h', '127.0.0.1', '-p', '15439', '-U', 'documentflow', database], env=env)
        ollama = start('ollama', [RUNTIME / 'ollama/ollama.exe', 'serve'])
        wait_http('http://127.0.0.1:11445/api/tags', ollama)
        api_dir = SOURCE / 'src/DocumentFlow.Api'
        api = start('api', ['dotnet', api_dir / 'bin/Debug/net10.0/DocumentFlow.Api.dll'], api_dir)
        wait_http(API_URL + '/health/live', api, 90)
        worker_dir = SOURCE / 'src/DocumentFlow.Worker'
        start('worker', ['dotnet', worker_dir / 'bin/Debug/net10.0/DocumentFlow.Worker.dll'], worker_dir)
        frontend = SOURCE / 'frontend/documentflow-web'
        web = start('web', ['node', frontend / 'node_modules/vite/bin/vite.js', '--host', '127.0.0.1', '--port', '5173', '--strictPort'], frontend)
        wait_http(WEB_URL, web)
        yield {'env': env, 'private': private, 'api': API_URL, 'web': WEB_URL, 'children': children, 'runtime': RUNTIME, 'database': database}
    finally:
        for child in reversed(children):
            if child.poll() is None:
                child.terminate()
                try:
                    child.wait(timeout=12)
                except subprocess.TimeoutExpired:
                    child.kill(); child.wait(timeout=5)
        for log in logs:
            log.close()
        if postgres_started:
            run([RUNTIME / 'pgsql/bin/pg_ctl.exe', '-D', RUNTIME / 'pgdata', '-m', 'fast', '-w', 'stop'], env=env)
        print('Owned stack processes stopped.', flush=True)


if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('--no-build', action='store_true')
    parser.add_argument('--no-browser', action='store_true')
    arguments = parser.parse_args()
    try:
        with stack(build=not arguments.no_build) as state:
            print('DocumentFlow: ' + WEB_URL, flush=True)
            print('Login: admin. Password: DocumentFlow-runtime/local-secrets.json (adminPassword).', flush=True)
            print('Ctrl+C stops API, Worker, frontend, Ollama and this PostgreSQL instance.', flush=True)
            if not arguments.no_browser:
                webbrowser.open(WEB_URL)
            while all(child.poll() is None for child in state['children']):
                time.sleep(1)
            raise RuntimeError('One of the services exited. Inspect runtime logs.')
    except KeyboardInterrupt:
        pass
    except Exception as error:
        print(str(error), flush=True)
        raise SystemExit(1)
