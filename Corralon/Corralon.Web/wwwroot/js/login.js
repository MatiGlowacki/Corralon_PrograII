// Login: valida las credenciales contra la API y guarda el token JWT.

iniciarPagina({ requiereLogin: false });

if (Sesion.obtener()) location.replace('home.html');

const aviso = document.getElementById('aviso');
function mostrarAviso(texto, tipo) {
  aviso.className = `alert alert-${tipo}`;
  aviso.textContent = texto;
}

if (new URLSearchParams(location.search).get('expirada')) {
  mostrarAviso('Tu sesión venció. Ingresá nuevamente.', 'warning');
}

document.getElementById('form-login').addEventListener('submit', async (e) => {
  e.preventDefault();
  const usuario = document.getElementById('usuario').value.trim();
  const password = document.getElementById('password').value;
  if (!usuario || !password) {
    mostrarAviso('Completá el usuario y la contraseña.', 'warning');
    return;
  }

  const boton = document.getElementById('btn-login');
  boton.disabled = true;
  boton.textContent = 'Verificando...';
  try {
    const resp = await api('/api/auth/login', {
      method: 'POST',
      body: { nombreUsuario: usuario, password },
      auth: false
    });
    Sesion.guardar(resp);
    location.href = 'home.html';
  } catch (err) {
    mostrarAviso(err.message, 'danger');
    boton.disabled = false;
    boton.textContent = 'Ingresar';
  }
});
