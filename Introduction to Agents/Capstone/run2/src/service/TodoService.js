import axios from 'axios';
import ax from './../components/AxiosInterceptor'

const TODO_API_BASE_URL = 'http://localhost:5000/api/TodoItems';

class ApiService
{
    getTodos()
    {
        //debugger;

        return ax.get(TODO_API_BASE_URL);
    }

    getTodoById(todoId)
    {
        return ax.get(TODO_API_BASE_URL + '/' + todoId);
    }

    deleteTodo(todoId)
    {
        return ax.delete(TODO_API_BASE_URL + '/' + todoId);
    }

    addTodo(todo)
    {
        return ax.post(TODO_API_BASE_URL, todo)
    }

    editTodo(todo)
    {
        return ax.put(TODO_API_BASE_URL + '/' + todo.id, todo);
    }

    importTodos(file)
    {
        const formData = new FormData();

        formData.append('file', file);

        return ax.post(TODO_API_BASE_URL + '/import', formData);
    }

}

export default new ApiService();