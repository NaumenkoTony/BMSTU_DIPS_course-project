import { Button, Center, Container, Paper, Title } from "@mantine/core";

function LoginPage() {
  const handleLogin = () => {
    const API_URL = import.meta.env.VITE_API_URL || "http://localhost:8080";
    window.location.href = `${API_URL}/api/v1/authorize/login`;
  };

  return (
    <Center mih="100vh">
      <Container size="sm">
        <Paper shadow="md" radius="lg" p="xl" withBorder>
          <Title order={2} mb="lg">
            Добро пожаловать
          </Title>
          <Button fullWidth size="lg" radius="md" onClick={handleLogin}>
            Войти через Identity
          </Button>
        </Paper>
      </Container>
    </Center>
  );
}

export default LoginPage;